﻿// <copyright>
// Copyright by BEMA Software Services
//
// Licensed under the Rock Community License (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
// http://www.rockrms.com/license
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.
// </copyright>

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data.Entity;
using System.IO;
using System.Linq;
using System.Text;
using System.Web.UI;
using System.Web.UI.WebControls;
using Rock;
using Rock.Attribute;
using Rock.Communication;
using Rock.Data;
using Rock.MergeTemplates;
using Rock.Model;
using Rock.Security;
using Rock.Web.Cache;
using Rock.Web.UI;
using Rock.Web.UI.Controls;

namespace RockWeb.Plugins.com_bemaservices.GroupTools
{
    [DisplayName( "Group Attendance Entry" )]
    [Category( "BEMA Services > Group Tools" )]
    [Description( "Lists the group members for a specific occurrence datetime and allows selecting if they attended or not." )]

    [GroupTypesField( "Allowed Group Types", "", false, "", "", 0, BemaAttributeKey.AllowedGroupTypes )]
    [WorkflowTypeField( "Workflow", "An optional workflow type to launch whenever attendance is saved. The Group will be used as the workflow 'Entity' when processing is started. Additionally if a 'StartDateTime' attribute exista, its value will be set with the corresponding saved attendance value.", false, false, "", "", 1, BemaAttributeKey.Workflow )]
    [NoteTypeField( "Note Types", "The Note Types that can be added to a person's profile", true, "Rock.Model.Person", "", "", false, "", "", 2, BemaAttributeKey.NoteTypes )]
    [WorkflowTypeField( "Note Workflow", "An optional workflow type to launch whenever a note is saved. The Note will be used as the workflow 'Entity' when processing is started. ", false, false, "", "", 3, BemaAttributeKey.NoteWorkflow )]
    [CustomRadioListField( "Default Attendance Type", "An optional default attendance type to use if one is not passed through the page parameters.", "In-person, Virtual, Mixed", false, "", "", 4, BemaAttributeKey.DefaultAttendanceType )]
    [BooleanField( "Show Inactive Members", "", false, "", 5, BemaAttributeKey.ShowInactiveMembers )]
    [BooleanField( "Show Pending Members", "", false, "", 6, BemaAttributeKey.ShowPendingMembers )]
    [TextField( "Success Text", "The text to display after an attendance has been saved.", true, "Attendance Saved", "", 7, BemaAttributeKey.SuccessText )]
    [DefinedValueField(
        "Connection Status",
        Key = BemaAttributeKey.ConnectionStatus,
        Description = "The connection status to use for new individuals (default = 'Web Prospect'.)",
        DefinedTypeGuid = "2E6540EA-63F0-40FE-BE50-F2A84735E600",
        IsRequired = true,
        AllowMultiple = false,
        DefaultValue = "368DD475-242C-49C4-A42C-7278BE690CC2",
        Order = 8 )]

    [DefinedValueField(
        "Record Status",
        Key = BemaAttributeKey.RecordStatus,
        Description = "The record status to use for new individuals (default = 'Pending'.)",
        DefinedTypeGuid = "8522BADD-2871-45A5-81DD-C76DA07E2E7E",
        IsRequired = true,
        AllowMultiple = false,
        DefaultValue = "283999EC-7346-42E3-B807-BCE9B2BABB49",
        Order = 9 )]

    [BooleanField( "Restrict Future Occurrence Date", "Should user be prevented from selecting a future Occurrence date?", false, "", 10, BemaAttributeKey.RestrictFutureOccurrenceDate )]
    [BooleanField( "Allow Sorting", "Should the block allow sorting the Member's list by First Name or Last Name?", true, "", 11, BemaAttributeKey.AllowSorting )]

    public partial class GroupAttendanceEntry : RockBlock
    {
        private readonly string _photoFormat = "<div class=\"photo-icon photo-round photo-round-xs pull-left margin-r-sm js-person-popover\" personid=\"{0}\" data-original=\"{1}&w=50\" style=\"background-image: url( '{2}' ); background-size: cover; background-repeat: no-repeat;\"></div>";

        /* BEMA.Start */
        #region Attribute Keys
        private static class BemaAttributeKey
        {
            public const string AllowedGroupTypes = "AllowedGroupTypes";
            public const string NoteTypes = "NoteTypes";
            public const string Workflow = "Workflow";
            public const string NoteWorkflow = "NoteWorkflow";
            public const string DefaultAttendanceType = "DefaultAttendanceType";
            public const string ShowInactiveMembers = "ShowInactiveMembers";
            public const string ShowPendingMembers = "ShowPendingMembers";
            public const string SuccessText = "SuccessText";
            public const string ConnectionStatus = "ConnectionStatus";
            public const string RecordStatus = "RecordStatus";
            public const string RestrictFutureOccurrenceDate = "RestrictFutureOccurrenceDate";
            public const string AllowSorting = "AllowSorting";
        }

        #endregion
        /* BEMA.End */

        #region Fields

        private static class AttendanceType
        {
            public const string Attended = "Attended";
            public const string InPerson = "In-person";
            public const string Virtual = "Virtual";
            public const string DidNotAttend = "Did not Attend";
        }

        #endregion

        #region Private Variables

        private RockContext _rockContext = null;
        private Group _group = null;
        private bool _canManageMembers = false;
        private AttendanceOccurrence _occurrence = null;
        private List<GroupAttendanceAttendee> _attendees;
        private string _attendanceType;
        private const string TOGGLE_SETTING = "Attendance_List_Sorting_Toggle";

        #endregion

        #region Control Methods

        protected override void LoadViewState( object savedState )
        {
            base.LoadViewState( savedState );
            _attendees = ViewState["Attendees"] as List<GroupAttendanceAttendee>;
            _attendanceType = ViewState["AttendanceType"] as string;
        }

        /// <summary>
        /// Raises the <see cref="E:System.Web.UI.Control.Init" /> event.
        /// </summary>
        /// <param name="e">An <see cref="T:System.EventArgs" /> object that contains the event data.</param>
        protected override void OnInit( EventArgs e )
        {
            base.OnInit( e );

            _rockContext = new RockContext();

            int groupId = PageParameter( "GroupId" ).AsInteger();
            _group = new GroupService( _rockContext )
                .Queryable( "GroupType,Schedule" ).AsNoTracking()
                .FirstOrDefault( g => g.Id == groupId );

            if ( _group != null && ( _group.IsAuthorized( Authorization.MANAGE_MEMBERS, CurrentPerson ) || _group.IsAuthorized( Authorization.EDIT, CurrentPerson ) ) )
            {
                lGroupHeading.Text = _group.Name;
                _canManageMembers = true;
            }


            var groupTypeGuids = GetAttributeValue( BemaAttributeKey.AllowedGroupTypes ).SplitDelimitedValues().AsGuidList();
            if ( groupTypeGuids.Any() && _group != null && !groupTypeGuids.Contains( _group.GroupType.Guid ) )
            {
                _canManageMembers = false;
            }

            var globalAttributesCache = GlobalAttributesCache.Get();
            var churchHeading = String.Format( @"<img src='{0}GetImage.ashx?id={1}&w=42&h=42' class='logo' /> {2}"
            , globalAttributesCache.GetValue( "PublicApplicationRoot" )
            , "{{ CurrentPage.Layout.Site.SiteLogoBinaryFileId }}"
            , globalAttributesCache.GetValue( "OrganizationName" ) );

            var mergeFields = Rock.Lava.LavaHelper.GetCommonMergeFields( this.RockPage );
            mergeFields.Add( "CurrentPage", this.PageCache );
            lChurchHeading.Text = churchHeading.ResolveMergeFields( mergeFields );

            dpOccurrenceDate.AllowFutureDateSelection = !GetAttributeValue( BemaAttributeKey.RestrictFutureOccurrenceDate ).AsBoolean();

            tglSort.Visible = GetAttributeValue( BemaAttributeKey.AllowSorting ).AsBooleanOrNull() ?? true;
        }

        /// <summary>
        /// Raises the <see cref="E:System.Web.UI.Control.Load" /> event.
        /// </summary>
        /// <param name="e">The <see cref="T:System.EventArgs" /> object that contains the event data.</param>
        protected override void OnLoad( EventArgs e )
        {
            base.OnLoad( e );

            if ( !Page.IsPostBack )
            {
                tglSort.Checked = GetUserPreference( TOGGLE_SETTING ).AsBoolean( true );
            }

            if ( !_canManageMembers )
            {
                nbNotice.Heading = "Sorry";
                nbNotice.Text = "<p>You're not authorized to update the attendance for the selected group.</p>";
                nbNotice.NotificationBoxType = NotificationBoxType.Danger;
                nbNotice.Visible = true;
                pnlDetails.Visible = false;
            }
            else
            {
                _occurrence = GetOccurrence();
                if ( !Page.IsPostBack )
                {
                    if ( PageParameter( "Type" ).IsNotNullOrWhiteSpace() )
                    {
                        _attendanceType = PageParameter( "Type" );
                    }
                    else
                    {
                        var defaultAttendanceType = GetAttributeValue( BemaAttributeKey.DefaultAttendanceType );
                        if ( defaultAttendanceType.IsNotNullOrWhiteSpace() )
                        {
                            _attendanceType = defaultAttendanceType;
                        }
                        else
                        {
                            ShowTypeModal();
                        }
                    }

                    ShowDetails();
                }
                else
                {
                    if ( _attendees != null )
                    {
                        foreach ( var item in lvMembers.Items )
                        {
                            var hfMember = item.FindControl( "hfMember" ) as HiddenField;
                            var rblAttendance = item.FindControl( "rblAttendance" ) as RockRadioButtonList;
                            var cbAttendance = item.FindControl( "cbAttendance" ) as RockCheckBox;

                            if ( hfMember != null && rblAttendance != null )
                            {
                                int personId = hfMember.ValueAsInt();

                                var attendance = _attendees.Where( a => a.PersonId == personId ).FirstOrDefault();
                                if ( attendance != null )
                                {
                                    if ( rblAttendance.SelectedValue.IsNullOrWhiteSpace() )
                                    {
                                        attendance.Attended = cbAttendance.Checked;
                                    }
                                    else
                                    {
                                        attendance.Attended = rblAttendance.SelectedValue != AttendanceType.DidNotAttend;

                                    }

                                    if ( _attendanceType == AttendanceType.InPerson || _attendanceType == AttendanceType.Virtual )
                                    {
                                        attendance.AttendanceType = _attendanceType;
                                    }
                                    else
                                    {
                                        if ( rblAttendance.SelectedValue == AttendanceType.InPerson || rblAttendance.SelectedValue == AttendanceType.Virtual )
                                        {
                                            attendance.AttendanceType = rblAttendance.SelectedValue;
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }

        private void ShowTypeModal()
        {
            pnlDetails.Visible = false;
            mdOccurrenceAttendanceType.Title = String.Format( "Attendance for {0}", _occurrence.OccurrenceDate.ToShortDateString() );
            mdOccurrenceAttendanceType.Show();
        }

        /// <summary>
        /// Saves any user control view-state changes that have occurred since the last page postback.
        /// </summary>
        /// <returns>
        /// Returns the user control's current view state. If there is no view state associated with the control, it returns null.
        /// </returns>
        protected override object SaveViewState()
        {
            ViewState["Attendees"] = _attendees;
            ViewState["AttendanceType"] = _attendanceType;
            return base.SaveViewState();
        }

        #endregion

        #region Events

        /// <summary>
        /// Handles the Click event of the lbSave control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        protected void lbSave_Click( object sender, EventArgs e )
        {
            if ( _group != null && _occurrence != null )
            {
                if ( SaveAttendance() )
                {

                    var qryParams = new Dictionary<string, string> { { "GroupId", _group.Id.ToString() } };

                    var groupTypeIds = PageParameter( "GroupTypeIds" );
                    if ( !string.IsNullOrWhiteSpace( groupTypeIds ) )
                    {
                        qryParams.Add( "GroupTypeIds", groupTypeIds );
                    }

                    NavigateToParentPage( qryParams );
                }
            }
        }

        /// <summary>
        /// Handles the Click event of the lbSave control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        protected void lbCancel_Click( object sender, EventArgs e )
        {
            if ( _group != null )
            {
                var qryParams = new Dictionary<string, string> { { "GroupId", _group.Id.ToString() } };

                var groupTypeIds = PageParameter( "GroupTypeIds" );
                if ( !string.IsNullOrWhiteSpace( groupTypeIds ) )
                {
                    qryParams.Add( "GroupTypeIds", groupTypeIds );
                }

                NavigateToParentPage( qryParams );
            }
        }

        /// <summary>
        /// Handles the CheckedChanged event of the tglSort UI control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        protected void tglSort_CheckedChanged( object sender, EventArgs e )
        {
            SetUserPreference( TOGGLE_SETTING, tglSort.Checked.ToString() );
            BindAttendees();
        }

        /// <summary>
        /// Handles the Command event of the lbMemberNote control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="CommandEventArgs"/> instance containing the event data.</param>
        protected void lbMemberNote_Command( object sender, CommandEventArgs e )
        {
            int? personId = e.CommandArgument.ToString().AsIntegerOrNull();

            if ( personId == null )
            {
                return;
            }

            var person = new PersonService( new RockContext() ).Get( personId.Value );
            if ( person == null )
            {
                return;
            }

            hfPersonId.Value = personId.ToString();
            rblNoteType.Items.Clear();
            var noteTypeGuids = GetAttributeValue( BemaAttributeKey.NoteTypes ).SplitDelimitedValues().AsGuidList();
            foreach ( var noteTypeGuid in noteTypeGuids )
            {
                var noteTypeCache = NoteTypeCache.Get( noteTypeGuid );
                if ( noteTypeCache != null )
                {
                    rblNoteType.Items.Add( new ListItem( noteTypeCache.Name, noteTypeCache.Id.ToString() ) );
                }
            }
            mdMemberNote.Title = String.Format( "Note For {0}", person.FullName );
            tbNote.Text = string.Empty;
            nbNote.Visible = false;
            mdMemberNote.Show();
        }

        /// <summary>
        /// Handles the ItemDataBound event of the lvMembers control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="ListViewItemEventArgs"/> instance containing the event data.</param>
        protected void lvMembers_ItemDataBound( object sender, ListViewItemEventArgs e )
        {
            var data = e.Item.DataItem as GroupAttendanceAttendee;
            CampusPicker fromCampus = e.Item.FindControl( "cpFromCampus" ) as CampusPicker;
            var hfMember = e.Item.FindControl( "hfMember" ) as HiddenField;
            var hfMemberName = e.Item.FindControl( "hfMemberName" ) as HiddenField;
            var lMember = e.Item.FindControl( "lMember" ) as Literal;
            var rblAttendance = e.Item.FindControl( "rblAttendance" ) as RockRadioButtonList;
            var cbAttendance = e.Item.FindControl( "cbAttendance" ) as RockCheckBox;

            rblAttendance.Items.Clear();
            switch ( _attendanceType )
            {
                case "In-person":
                    rblAttendance.Visible = false;
                    cbAttendance.Visible = true;
                    cbAttendance.Checked = data.Attended;
                    break;
                case "Virtual":
                    rblAttendance.Visible = false;
                    cbAttendance.Visible = true;
                    cbAttendance.Checked = data.Attended;
                    break;
                default:
                    rblAttendance.Items.Add( AttendanceType.InPerson );
                    rblAttendance.Items.Add( AttendanceType.Virtual );
                    rblAttendance.Items.Add( AttendanceType.DidNotAttend );

                    if ( !data.Attended )
                    {
                        rblAttendance.SelectedValue = AttendanceType.DidNotAttend;
                    }
                    else
                    {
                        rblAttendance.SelectedValue = data.AttendanceType;
                    }
                    break;
            }

            string displayName = string.Empty;

            hfMember.SetValue( data.PersonId );
            hfMemberName.Value = data.FullName;

            if ( data != null )
            {
                StringBuilder sbNameHtml = new StringBuilder();
                sbNameHtml.AppendFormat( _photoFormat, data.PersonId, data.PhotoUrl, data.PhotoUrl );

                if ( tglSort.Visible && tglSort.Checked )
                {
                    sbNameHtml.Append( data.LastName + ", " + data.NickName );
                }
                else
                {
                    sbNameHtml.Append( data.NickName + " " + data.LastName );
                }

                if ( !_group.Members.Where( m => m.PersonId == data.PersonId && m.GroupMemberStatus == GroupMemberStatus.Active ).Any() )
                {
                    if ( _group.Members.Where( m => m.PersonId == data.PersonId && m.GroupMemberStatus == GroupMemberStatus.Pending ).Any() )
                    {
                        sbNameHtml.Append( " <small>(Pending)</small>" );
                    }

                    if ( _group.Members.Where( m => m.PersonId == data.PersonId && m.GroupMemberStatus == GroupMemberStatus.Inactive ).Any() )
                    {
                        sbNameHtml.Append( " <small>(Inactive)</small>" );
                    }
                }

                lMember.Text = sbNameHtml.ToString();
            }
        }

        /// <summary>
        /// Handles the SaveClick event of the mdOccurrenceAttendanceType control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        protected void mdOccurrenceAttendanceType_SaveClick( object sender, EventArgs e )
        {
            mdOccurrenceAttendanceType.Hide();
            _attendanceType = rblOccurrenceAttendanceType.SelectedValue;
            if ( _attendanceType == "DidNotMeet" )
            {
                SaveAttendance( false );
                var qryParams = new Dictionary<string, string> { { "GroupId", _group.Id.ToString() } };

                var groupTypeIds = PageParameter( "GroupTypeIds" );
                if ( !string.IsNullOrWhiteSpace( groupTypeIds ) )
                {
                    qryParams.Add( "GroupTypeIds", groupTypeIds );
                }

                NavigateToParentPage( qryParams );
            }
            else
            {
                pnlDetails.Visible = true;
                ShowDetails();
            }
        }

        /// <summary>
        /// Handles the SaveClick event of the mdMemberNote control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        protected void mdMemberNote_SaveClick( object sender, EventArgs e )
        {
            using ( var rockContext = new RockContext() )
            {
                var personService = new PersonService( rockContext );
                var noteService = new NoteService( rockContext );
                var personId = hfPersonId.Value.AsIntegerOrNull();
                var noteTypeId = rblNoteType.SelectedValueAsId();
                if ( personId != null && noteTypeId != null && tbNote.Text.IsNotNullOrWhiteSpace() )
                {
                    var person = personService.Get( personId.Value );
                    if ( person != null )
                    {
                        Note note = new Note();
                        note.EntityId = person.Id;
                        note.IsPrivateNote = false;
                        note.Text = tbNote.Text;

                        var noteType = NoteTypeCache.Get( noteTypeId.Value );
                        if ( noteType != null )
                        {
                            note.NoteTypeId = noteType.Id;
                        }

                        // get author
                        var author = CurrentPerson;
                        if ( author != null )
                        {
                            note.CreatedByPersonAliasId = author.PrimaryAlias.Id;
                        }

                        noteService.Add( note );
                        rockContext.SaveChanges();

                        Guid? workflowTypeGuid = GetAttributeValue( BemaAttributeKey.NoteWorkflow ).AsGuidOrNull();
                        if ( workflowTypeGuid.HasValue )
                        {
                            var workflowType = WorkflowTypeCache.Get( workflowTypeGuid.Value );
                            if ( workflowType != null && ( workflowType.IsActive ?? true ) )
                            {
                                try
                                {
                                    var workflow = Workflow.Activate( workflowType, person.FullName );

                                    List<string> workflowErrors;
                                    new WorkflowService( rockContext ).Process( workflow, note, out workflowErrors );
                                }
                                catch ( Exception ex )
                                {
                                    ExceptionLogService.LogException( ex, this.Context );
                                }
                            }
                        }

                        mdMemberNote.Hide();
                    }
                }
                else
                {
                    nbNote.Visible = true;
                }
            }
        }

        /// <summary>
        /// Handles the SaveClick event of the mdAddPerson control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        protected void mdAddPerson_SaveClick( object sender, EventArgs e )
        {
            using ( var rockContext = new RockContext() )
            {
                string firstName = tbFirstName.Text;
                string lastName = tbLastName.Text;
                string email = tbEmail.Text;
                if ( firstName.IsNotNullOrWhiteSpace() && lastName.IsNotNullOrWhiteSpace() && email.IsNotNullOrWhiteSpace() )
                {
                    Person person = null;
                    PersonAlias personAlias = null;
                    var personService = new PersonService( rockContext );

                    var personQuery = new PersonService.PersonMatchQuery( firstName, lastName, email, "" );
                    person = personService.FindPerson( personQuery, true );

                    if ( person.IsNotNull() )
                    {
                        personAlias = person.PrimaryAlias;
                    }
                    else
                    {
                        // Add New Person
                        person = new Person();
                        person.FirstName = firstName.FixCase();
                        person.LastName = lastName.FixCase();
                        person.IsEmailActive = true;
                        person.Email = email;
                        person.EmailPreference = EmailPreference.EmailAllowed;
                        person.RecordTypeValueId = DefinedValueCache.Get( Rock.SystemGuid.DefinedValue.PERSON_RECORD_TYPE_PERSON.AsGuid() ).Id;

                        var defaultConnectionStatus = DefinedValueCache.Get( GetAttributeValue( BemaAttributeKey.ConnectionStatus ).AsGuid() );
                        if ( defaultConnectionStatus != null )
                        {
                            person.ConnectionStatusValueId = defaultConnectionStatus.Id;
                        }

                        var defaultRecordStatus = DefinedValueCache.Get( GetAttributeValue( BemaAttributeKey.RecordStatus ).AsGuid() );
                        if ( defaultRecordStatus != null )
                        {
                            person.RecordStatusValueId = defaultRecordStatus.Id;
                        }

                        var familyGroup = PersonService.SaveNewPerson( person, rockContext );
                        if ( familyGroup != null && familyGroup.Members.Any() )
                        {
                            person = familyGroup.Members.Select( m => m.Person ).First();
                            personAlias = person.PrimaryAlias;
                        }
                    }

                    if ( person != null && personAlias != null )
                    {
                        int? groupRoleId = null;

                        if ( !groupRoleId.HasValue && _group != null )
                        {
                            // use the group's grouptype's default group role if a group role wasn't specified
                            groupRoleId = _group.GroupType.DefaultGroupRoleId;
                        }

                        var status = GroupMemberStatus.Active;

                        var groupMemberService = new GroupMemberService( rockContext );
                        var groupMember = groupMemberService.GetByGroupIdAndPersonIdAndPreferredGroupRoleId( _group.Id, person.Id, groupRoleId.Value );
                        bool isNew = false;
                        if ( groupMember == null )
                        {
                            groupMember = new GroupMember();
                            groupMember.PersonId = person.Id;
                            groupMember.GroupId = _group.Id;
                            groupMember.GroupRoleId = groupRoleId.Value;
                            groupMember.GroupMemberStatus = status;
                            isNew = true;
                        }

                        if ( groupMember.IsValidGroupMember( rockContext ) )
                        {
                            if ( isNew )
                            {
                                groupMemberService.Add( groupMember );
                            }
                            rockContext.SaveChanges();
                        }
                    }

                    ShowDetails();
                    mdAddPerson.Hide();
                }
                else
                {
                    nbPerson.Visible = true;
                }
            }
        }

        /// <summary>
        /// Handles the Click event of the lbAddPerson control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        protected void lbAddPerson_Click( object sender, EventArgs e )
        {
            tbFirstName.Text = string.Empty;
            tbLastName.Text = string.Empty;
            tbEmail.Text = string.Empty;
            nbPerson.Visible = false;
            mdAddPerson.Show();
        }

        /// <summary>
        /// Handles the SelectDate event of the dpOccurrenceDate control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        protected void dpOccurrenceDate_SelectDate( object sender, EventArgs e )
        {
            _occurrence = GetOccurrence();
            ShowDetails();
        }

        #endregion

        #region Internal Methods

        /// <summary>
        /// Gets the occurrence items.
        /// </summary>
        private AttendanceOccurrence GetOccurrence()
        {
            AttendanceOccurrence occurrence = null;

            var occurrenceService = new AttendanceOccurrenceService( _rockContext );

            // Check to see if a occurrence id was specified on the query string, and if so, query for it
            int? occurrenceId = PageParameter( "OccurrenceId" ).AsIntegerOrNull();
            if ( occurrenceId.HasValue && occurrenceId.Value > 0 )
            {
                occurrence = occurrenceService.Get( occurrenceId.Value );

                // If we have a valid occurrence return it now (the date,location,schedule cannot be changed for an existing occurrence)
                if ( occurrence != null )
                {
                    return occurrence;
                }
            }

            // Set occurrence values from query string
            DateTime? occurrenceDate = ( PageParameter( "Date" ).AsDateTime() ?? PageParameter( "Occurrence" ).AsDateTime() ) ?? RockDateTime.Today;
            var locationId = PageParameter( "LocationId" ).AsIntegerOrNull();
            var scheduleId = PageParameter( "ScheduleId" ).AsIntegerOrNull();

            if ( scheduleId == null )
            {
                // if no specific schedule was specified in the URL, use the group's scheduleId 
                scheduleId = _group.ScheduleId;
            }

            // If this is a postback, check to see if date/location/schedule were updated
            if ( Page.IsPostBack )
            {
                if ( dpOccurrenceDate.Visible && dpOccurrenceDate.SelectedDate.HasValue )
                {
                    occurrenceDate = dpOccurrenceDate.SelectedDate.Value;
                }
            }

            if ( occurrence == null && occurrenceDate.HasValue )
            {
                // if no specific occurrenceId was specified, try to find a matching occurrence from Date, GroupId, Location, ScheduleId
                occurrence = occurrenceService.Get( occurrenceDate.Value.Date, _group.Id, locationId, scheduleId );
            }

            // If an occurrence date was included, but no occurrence was found with that date, and new 
            // occurrences can be added, create a new one
            if ( occurrence == null )
            {
                // Create a new occurrence record and return it
                return new AttendanceOccurrence
                {
                    Group = _group,
                    GroupId = _group.Id,
                    OccurrenceDate = occurrenceDate ?? RockDateTime.Today,
                    LocationId = locationId,
                    ScheduleId = scheduleId,
                };
            }

            return occurrence;
        }

        /// <summary>
        /// Binds the group members grid.
        /// </summary>
        protected void ShowDetails()
        {
            if ( _occurrence == null )
            {
                nbNotice.Heading = "No Occurrences";
                nbNotice.Text = "<p>There are currently not any active occurrences for selected group to take attendance for.</p>";
                nbNotice.NotificationBoxType = NotificationBoxType.Warning;
                nbNotice.Visible = true;

                pnlDetails.Visible = false;
            }
            else
            {
                nbNotice.Visible = false;

                if ( PageParameter( "OccurrenceId" ).AsIntegerOrNull().HasValue )
                {
                    lOccurrenceDate.Visible = true;
                    lOccurrenceDate.Text = _occurrence.OccurrenceDate.ToShortDateString();
                    dpOccurrenceDate.Visible = false;
                }
                else
                {
                    lOccurrenceDate.Visible = false;
                    dpOccurrenceDate.Visible = true;
                    dpOccurrenceDate.SelectedDate = _occurrence.OccurrenceDate;
                }

                lMembers.Text = _group.GroupType.GroupMemberTerm.Pluralize();

                List<int> attendedIds = new List<int>();
                Dictionary<int, string> attendedIdTypes = new Dictionary<int, string>();
                // Load the attendance for the selected occurrence
                if ( _occurrence.Id > 0 )
                {
                    var attendanceList = new AttendanceService( _rockContext )
                        .Queryable().AsNoTracking()
                        .Where( a =>
                            a.OccurrenceId == _occurrence.Id &&
                            a.DidAttend.HasValue &&
                            a.DidAttend.Value &&
                            a.PersonAlias != null )
                        .ToList();

                    attendanceList.ForEach( a => a.LoadAttributes( _rockContext ) );

                    attendedIdTypes = attendanceList.Select( a => new
                    {
                        PersonId = a.PersonAlias.PersonId,
                        AttendanceType = a.GetAttributeValue( "AttendanceType" ) ?? ""
                    } )
                    .GroupBy( a => a.PersonId )
                    .ToDictionary( x => x.Key, x => x.First().AttendanceType );

                    // Get the list of people who attended
                    attendedIds = attendanceList
                        .Select( a => a.PersonAlias.PersonId )
                        .Distinct()
                        .ToList();
                }

                // Get the group members
                var groupMemberService = new GroupMemberService( _rockContext );

                // Add any existing members not on that list
                var showInactiveMembers = GetAttributeValue( BemaAttributeKey.ShowInactiveMembers ).AsBoolean();
                var showPendingMembers = GetAttributeValue( BemaAttributeKey.ShowPendingMembers ).AsBoolean();

                var unattendedIds = groupMemberService
                    .Queryable().AsNoTracking()
                    .Where( m =>
                        m.GroupId == _group.Id &&
                        !attendedIds.Contains( m.PersonId ) &&
                        (
                            m.GroupMemberStatus == GroupMemberStatus.Active ||
                            ( m.GroupMemberStatus == GroupMemberStatus.Inactive && showInactiveMembers ) ||
                            ( m.GroupMemberStatus == GroupMemberStatus.Pending && showPendingMembers )
                        ) )
                    .Select( m => m.PersonId )
                    .ToList();

                var mergeFields = Rock.Lava.LavaHelper.GetCommonMergeFields( null );

                // Bind the attendance roster
                _attendees = new PersonService( _rockContext )
                    .Queryable().AsNoTracking()
                    .Where( p => attendedIds.Contains( p.Id ) || unattendedIds.Contains( p.Id ) )
                    .ToList()
                    .Select( p => new GroupAttendanceAttendee()
                    {
                        PersonId = p.Id,
                        PhotoUrl = p.PhotoUrl,
                        NickName = p.NickName,
                        LastName = p.LastName,
                        Attended = attendedIds.Contains( p.Id ),
                        AttendanceType = attendedIdTypes.ContainsKey( p.Id ) ? attendedIdTypes[p.Id] : "",
                        CampusIds = p.GetCampusIds()
                    } )
                    .ToList();

                BindAttendees();
            }
        }

        /// <summary>
        /// Binds the attendees to the list.
        /// </summary>
        private void BindAttendees()
        {
            int attendanceCount = _attendees.Where( a => a.Attended ).Count();

            if ( tglSort.Visible && tglSort.Checked )
            {
                lvMembers.DataSource = _attendees.OrderBy( a => a.LastName ).ThenBy( a => a.NickName ).ToList();
            }
            else
            {
                lvMembers.DataSource = _attendees.OrderBy( a => a.NickName ).ThenBy( a => a.LastName ).ToList();
            }

            lvMembers.DataBind();
        }

        /// <summary>
        /// Method to save attendance for use in two separate areas.
        /// </summary>
        protected bool SaveAttendance( bool didMeet = true )
        {
            using ( var rockContext = new RockContext() )
            {
                var occurrenceService = new AttendanceOccurrenceService( rockContext );
                var attendanceService = new AttendanceService( rockContext );
                var personAliasService = new PersonAliasService( rockContext );
                var locationService = new LocationService( rockContext );

                AttendanceOccurrence occurrence = null;

                if ( _occurrence.Id != 0 )
                {
                    occurrence = occurrenceService.Get( _occurrence.Id );
                }

                if ( occurrence == null )
                {
                    var existingOccurrence = occurrenceService.Get( _occurrence.OccurrenceDate, _group.Id, _occurrence.LocationId, _occurrence.ScheduleId );
                    if ( existingOccurrence != null )
                    {
                        nbNotice.Heading = "Occurrence Already Exists";
                        nbNotice.Text = "<p>An occurrence already exists for this group for the selected date, location, and schedule that you've selected. Please return to the list and select that occurrence to update it's attendance.</p>";
                        nbNotice.NotificationBoxType = NotificationBoxType.Danger;
                        nbNotice.Visible = true;

                        return false;
                    }
                    else
                    {
                        occurrence = new AttendanceOccurrence();
                        occurrence.GroupId = _occurrence.GroupId;
                        occurrence.LocationId = _occurrence.LocationId;
                        occurrence.ScheduleId = _occurrence.ScheduleId;
                        occurrence.OccurrenceDate = _occurrence.OccurrenceDate;
                        occurrenceService.Add( occurrence );
                    }
                }

                occurrence.Notes = string.Empty;
                occurrence.DidNotOccur = !didMeet;

                var existingAttendees = occurrence.Attendees.ToList();

                // If did not meet was selected and this was a manually entered occurrence (not based on a schedule/location)
                // then just delete all the attendance records instead of tracking a 'did not meet' value
                if ( !didMeet && !_occurrence.ScheduleId.HasValue )
                {
                    foreach ( var attendance in existingAttendees )
                    {
                        attendanceService.Delete( attendance );
                    }
                }
                else
                {
                    int? campusId = locationService.GetCampusIdForLocation( _occurrence.LocationId ) ?? _group.CampusId;

                    if ( !didMeet )
                    {
                        // If the occurrence is based on a schedule, set the did not meet flags
                        foreach ( var attendance in existingAttendees )
                        {
                            attendance.DidAttend = null;
                        }
                    }
                    else
                    {
                        _occurrence.Schedule = _occurrence.Schedule == null && _occurrence.ScheduleId.HasValue ? new ScheduleService( rockContext ).Get( _occurrence.ScheduleId.Value ) : _occurrence.Schedule;

                        cvAttendance.IsValid = _occurrence.IsValid;
                        if ( !cvAttendance.IsValid )
                        {
                            cvAttendance.ErrorMessage = _occurrence.ValidationResults.Select( a => a.ErrorMessage ).ToList().AsDelimited( "<br />" );
                            return false;
                        }

                        foreach ( var attendee in _attendees )
                        {
                            var attendance = existingAttendees
                                .Where( a => a.PersonAlias.PersonId == attendee.PersonId )
                                .FirstOrDefault();

                            if ( attendance == null )
                            {
                                int? personAliasId = personAliasService.GetPrimaryAliasId( attendee.PersonId );
                                if ( personAliasId.HasValue )
                                {
                                    attendance = new Attendance();
                                    attendance.PersonAliasId = personAliasId;
                                    attendance.CampusId = campusId;
                                    attendance.StartDateTime = _occurrence.Schedule != null && _occurrence.Schedule.HasSchedule() ? _occurrence.OccurrenceDate.Date.Add( _occurrence.Schedule.StartTimeOfDay ) : _occurrence.OccurrenceDate;
                                    attendance.DidAttend = attendee.Attended;


                                    // Check that the attendance record is valid
                                    cvAttendance.IsValid = attendance.IsValid;
                                    if ( !cvAttendance.IsValid )
                                    {
                                        cvAttendance.ErrorMessage = attendance.ValidationResults.Select( a => a.ErrorMessage ).ToList().AsDelimited( "<br />" );
                                        return false;
                                    }

                                    occurrence.Attendees.Add( attendance );

                                    rockContext.SaveChanges();
                                }
                            }
                            else
                            {
                                // Otherwise, only record that they attended -- don't change their attendance startDateTime 
                                attendance.DidAttend = attendee.Attended;
                                rockContext.SaveChanges();
                            }

                            if ( attendee.AttendanceType.IsNotNullOrWhiteSpace() )
                            {
                                attendance.LoadAttributes();
                                attendance.SetAttributeValue( "AttendanceType", attendee.AttendanceType );
                                attendance.SaveAttributeValues();
                            }
                        }
                    }
                }

                if ( occurrence.LocationId.HasValue )
                {
                    Rock.CheckIn.KioskLocationAttendance.Remove( occurrence.LocationId.Value );
                }

                Guid? workflowTypeGuid = GetAttributeValue( BemaAttributeKey.Workflow ).AsGuidOrNull();
                if ( workflowTypeGuid.HasValue )
                {
                    var workflowType = WorkflowTypeCache.Get( workflowTypeGuid.Value );
                    if ( workflowType != null && ( workflowType.IsActive ?? true ) )
                    {
                        try
                        {
                            var workflow = Workflow.Activate( workflowType, _group.Name );

                            workflow.SetAttributeValue( "StartDateTime", _occurrence.OccurrenceDate.ToString( "o" ) );
                            workflow.SetAttributeValue( "Schedule", _group.Schedule.Guid.ToString() );

                            List<string> workflowErrors;
                            new WorkflowService( rockContext ).Process( workflow, _group, out workflowErrors );
                        }
                        catch ( Exception ex )
                        {
                            ExceptionLogService.LogException( ex, this.Context );
                        }
                    }
                }

                _occurrence.Id = occurrence.Id;
            }

            return true;
        }

        #endregion

        #region Helper Classes

        [Serializable]
        public class GroupAttendanceAttendee
        {
            public int PersonId { get; set; }

            public string PhotoUrl { get; set; }

            public string NickName { get; set; }

            public string LastName { get; set; }

            public string FullName
            {
                get { return NickName + " " + LastName; }
            }

            public bool Attended { get; set; }

            public string AttendanceType { get; set; }

            public List<int> CampusIds { get; set; }
        }

        #endregion
    }
}