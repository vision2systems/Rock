﻿// <copyright>
// Copyright by the Spark Development Network
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

namespace Rock.Plugin.HotFixes
{
    /// <summary>
    /// Plug-in migration
    /// </summary>
    /// <seealso cref="Rock.Plugin.Migration" />
    [MigrationNumber( 96, "1.9.0" )]
    class MigrationRollupsForV9_5 : Migration
    {
        /// <summary>
        /// Operations to be performed during the upgrade process.
        /// </summary>
        public override void Up()
        {
            SetAssessmentAttributeValuesBlockSettings_Up();
            FixDQSocialMediaLinks();
            DisableInteractionSessionsListPageSearchIndex();
            ChartJsLavaShortCode();
            FixDefinedTypeDescriptionFormatting();
        }

        /// <summary>
        /// Operations to be performed during the downgrade process.
        /// </summary>
        public override void Down()
        {
            // Down migrations are not yet supported in plug-in migrations.
        }

        /// <summary>
        /// DL: Set Block Title and Icon Attributes for Person/ExtendedAttributes/Assessments block.
        /// This is a backport of a migration fix first released in v1.10.0 (201907241959453_Rollup_0724.cs).
        /// </summary>
        private void SetAssessmentAttributeValuesBlockSettings_Up()
        {
            // Attrib for BlockType: Attribute Values:Block Title
            RockMigrationHelper.UpdateBlockTypeAttribute( "D70A59DC-16BE-43BE-9880-59598FA7A94C", "9C204CD0-1233-41C5-818A-C5DA439445AA", "Block Title", "BlockTitle", null, @"The text to display as the heading.", 3, @"", "6AA30A6D-D5E7-482E-A207-33026070DB42" );
            // Attrib for BlockType: Attribute Values:Block Icon
            RockMigrationHelper.UpdateBlockTypeAttribute( "D70A59DC-16BE-43BE-9880-59598FA7A94C", "9C204CD0-1233-41C5-818A-C5DA439445AA", "Block Icon", "BlockIcon", null, @"The css class name to use for the heading icon.", 4, @"", "E7704D69-2B48-490A-AC40-80D112E6EE0A" );
            // Attrib Value for Block:Assessments, Attribute:Block Icon Page: Extended Attributes, Site: Rock RMS
            RockMigrationHelper.AddBlockAttributeValue( "0C244AA1-2473-4749-8D7E-81CAA415C886", "E7704D69-2B48-490A-AC40-80D112E6EE0A", @"fa fa-project-diagram" );
            // Attrib Value for Block:Assessments, Attribute:Block Title Page: Extended Attributes, Site: Rock RMS
            RockMigrationHelper.AddBlockAttributeValue( "0C244AA1-2473-4749-8D7E-81CAA415C886", "6AA30A6D-D5E7-482E-A207-33026070DB42", @"Assessments" );
        }

        /// <summary>
        /// GJ: Fix double quoted Social Media 
        /// </summary>
        private void FixDQSocialMediaLinks()
        {
            RockMigrationHelper.UpdateAttributeQualifier( Rock.SystemGuid.Attribute.PERSON_FACEBOOK, "texttemplate", "<a href='{{value}}' target='_blank'>{{ value | Url:'segments' | Last }}</a>", "BC8F9FEF-59D6-4BC4-84B3-BC6EC52CECED" );
            RockMigrationHelper.UpdateAttributeQualifier( Rock.SystemGuid.Attribute.PERSON_TWITTER, "texttemplate", "<a href='{{value}}' target='_blank'>{{ value | Url:'segments' | Last }}</a>", "6FFF488B-C7A8-410A-ADC2-3D9D21706511" );
            RockMigrationHelper.UpdateAttributeQualifier( Rock.SystemGuid.Attribute.PERSON_INSTAGRAM, "texttemplate", "<a href='{{value}}' target='_blank'>{{ value | Url:'segments' | Last }}</a>", "02820F4F-476A-448F-A869-14206625670C" );
            RockMigrationHelper.UpdateAttributeQualifier( Rock.SystemGuid.Attribute.PERSON_SNAPCHAT, "texttemplate", "<a href='{{value}}' target='_blank'>{{ value | Url:'segments' | Last }}</a>", "7B3650EF-8F42-40DF-A729-9BEF19941DD8" );
        }

        /// <summary>
        /// ED: Don't allow indexing on by default on Interaction Sessions List page
        /// </summary>
        private void DisableInteractionSessionsListPageSearchIndex()
        {
            Sql( @" UPDATE [Page] SET AllowIndexing = 0 WHERE [Guid] = '756D37B7-7BE2-497D-8D37-CC273FE29659'" );
        }

        /// <summary>
        /// GJ: Fix ChartJS Lava Shortcode
        /// </summary>
        private void ChartJsLavaShortCode()
        {
            Sql( HotFixMigrationResource._096_MigrationRollupsForV9_5_chartjsfix );
        }

        /// <summary>
        /// GJ: Fix Defined Type Description formatting
        /// </summary>
        private void FixDefinedTypeDescriptionFormatting()
        {
            Sql( @"
            UPDATE [DefinedType] SET [Description]=N'By default, Rock does not share saved login information across domains. For example if a user logs in from <i>http://<strong>www</strong>.rocksolidchurch.com</i>, they would also have to login at <i>http://<strong>admin</strong>.rocksolidchurch.com</i>. You can override this behavior so that all hosts of common domain share their login status. So in the case above, if <i>rocksolidchurchdemo.com</i> was entered below, logging into the <strong>www</strong> site would also auto log you into the <strong>admin</strong> site.' WHERE ([Guid]='6CE00E1B-FE09-45FE-BD9D-56C57A11BE1A')
            UPDATE [DefinedType] SET [Description]=N'Lists the external domains that are authorized to access the REST API through ""cross-origin resource sharing"" (CORS).' WHERE ([Guid]='DF7C8DF7-49F9-4858-9E5D-20842AF65AD8')
            UPDATE [DefinedType] SET [Description]=N'Defines preset colors shown inside of Color Picker controls.' WHERE ([Guid]='CC1400B3-E161-45E3-BF49-49825D3D6467')
            UPDATE [DefinedType] SET [Description]=N'Used by Rock''s Conflict Profile Assessment to hold all Conflict Themes.' WHERE ([Guid]='EE7E089E-DF81-4407-8BFA-AD865FA5427A')" );
        }

    }
}
