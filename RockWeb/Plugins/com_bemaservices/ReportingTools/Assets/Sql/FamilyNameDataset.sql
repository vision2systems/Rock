
	Declare @ModifiedDate datetime = null;
    DECLARE @cACTIVE_RECORD_STATUS_VALUE_GUID UNIQUEIDENTIFIER = '618F906C-C33D-4FA3-8AEF-E58CB7B63F1E'
    DECLARE @cPERSON_RECORD_TYPE_VALUE_GUID UNIQUEIDENTIFIER = '36CF10D6-C695-413D-8E7C-4546EFEF385E'
    DECLARE @cFAMILY_GROUPTYPE_GUID UNIQUEIDENTIFIER = '790E3215-3B10-442B-AF69-616C0DCB998E'
    DECLARE @cADULT_ROLE_GUID UNIQUEIDENTIFIER = '2639F9A5-2AAE-4E48-A8C3-4FFE86681E42'
    DECLARE @cTRANSACTION_TYPE_CONTRIBUTION UNIQUEIDENTIFIER = '2D607262-52D6-4724-910D-5C6E8FB89ACC';

    DECLARE @PersonRecordTypeValueId INT = (
            SELECT TOP 1 [Id]
            FROM [DefinedValue]
            WHERE [Guid] = @cPERSON_RECORD_TYPE_VALUE_GUID
            )
    DECLARE @FamilyGroupTypeId INT = (
            SELECT TOP 1 [Id]
            FROM [GroupType]
            WHERE [Guid] = @cFAMILY_GROUPTYPE_GUID
            )
    DECLARE @AdultRoleId INT = (
            SELECT TOP 1 [Id]
            FROM [GroupTypeRole]
            WHERE [Guid] = @cADULT_ROLE_GUID
            )
    DECLARE @ContributionType INT = (
            SELECT TOP 1 [Id]
            FROM [DefinedValue]
            WHERE [Guid] = @cTRANSACTION_TYPE_CONTRIBUTION
            )

    Declare @ModifiedPersonIds table(
			PersonId int,
			GroupId int null
			)

	Insert into @ModifiedPersonIds
	Select p.Id, familyMembers.GroupId
	From Person modifiedPerson
	Left Join (
			Select fm1.PersonId as modifiedFamilyMemberId, 
					g.Id as GroupId,
					fm2.PersonId as familyMemberId
			From GroupMember fm1
			Join [Group] g on g.Id = fm1.GroupId and g.GroupTypeId = @FamilyGroupTypeId and g.IsActive = 1 and g.IsArchived = 0
			Join GroupMember fm2 on fm2.GroupId = g.Id and fm2.GroupMemberStatus = 1
			Join GroupTypeRole gtr on fm2.GroupRoleId = gtr.Id
			Where gtr.Id = @AdultRoleId
		) familyMembers on familyMembers.modifiedFamilyMemberId = modifiedPerson.Id
	Join Person p on p.Id = familyMembers.familyMemberId
	Where (@ModifiedDate is null or modifiedPerson.ModifiedDateTime >= @ModifiedDate)


	Select	p.Id,
			mpId.GroupId,
			p.FirstName,
			p.NickName,
			p.LastName,
			case when p.Id = [dbo].[BEMA_ReportingTools_ufn_GetHeadOfHousePersonIdFromPersonId](p.id) then 'True' else 'False' end as Family_HeadofHousehold,
			case when p.Id = [dbo].[BEMA_ReportingTools_ufn_GetGivingUnitHeadOfHousePersonIdFromGivingId](p.GivingId) then 'True' else 'False' end as GivingUnit_HeadofHousehold,
			(SELECT * FROM dbo.ufnCrm_GetFamilyTitle( null, mpId.GroupId, default, 1)) 
				as Family_FullNameNickNameNoTitle,
			(SELECT * FROM dbo.ufnCrm_GetFamilyTitle( null, mpId.GroupId, (Select String_agg(p1.Id,',') From Person p1 where p1.GivingId = p.GivingId), 1) )
				as GivingUnit_FullNameNickNameNoTitle,
			(SELECT * FROM dbo.[BEMA_ReportingTools_ufn_GetFamilyNickNames](null, mpId.GroupId, default, 0))
				as Family_FirstName,
			(SELECT * FROM dbo.[BEMA_ReportingTools_ufn_GetFamilyNickNames]( null, mpId.GroupId, (Select String_agg(p1.Id,',') From Person p1 where p1.GivingId = p.GivingId), 0) )
				as GivingUnit_FirstName,
			(SELECT * FROM dbo.BEMA_ReportingTools_ufn_GetFamilyTitleFormal(null, mpId.GroupId, default, 1)) 
				as Family_FullNameNickName,
			(SELECT * FROM dbo.BEMA_ReportingTools_ufn_GetFamilyTitleFormal( null, mpId.GroupId, (Select String_agg(p1.Id,',') From Person p1 where p1.GivingId = p.GivingId), 1) )
				as GivingUnit_FullNameNickName,
			(SELECT * FROM dbo.[BEMA_ReportingTools_ufn_GetFamilyLastNames](null, mpId.GroupId, default, 1))
				as Family_LastNames,
			(SELECT * FROM dbo.[BEMA_ReportingTools_ufn_GetFamilyLastNames](null, mpId.GroupId, (Select String_agg(p1.Id,',') From Person p1 where p1.GivingId = p.GivingId), 1))
				as GivingUnit_LastNames,
			(SELECT * FROM dbo.[BEMA_ReportingTools_ufn_GetFamilyNickNames](null, mpId.GroupId, default, 1))
				as Family_NickNames,
			(SELECT * FROM dbo.[BEMA_ReportingTools_ufn_GetFamilyNickNames](null, mpId.GroupId, (Select String_agg(p1.Id,',') From Person p1 where p1.GivingId = p.GivingId), 1))
				as GivingUnit_NickNames,
			(SELECT * FROM dbo.[BEMA_ReportingTools_ufn_GetFamilyTitles](null, mpId.GroupId, default, 1)) 
				as Family_Titles,
			(SELECT * FROM dbo.[BEMA_ReportingTools_ufn_GetFamilyTitles](null, mpId.GroupId, (Select String_agg(p1.Id,',') From Person p1 where p1.GivingId = p.GivingId), 1)) 
				as GivingUnit_Titles,
			(SELECT * FROM dbo.BEMA_ReportingTools_ufn_GetFamilyTitleFormal(null, mpId.GroupId, default, 0)) 
				as Family_FullNameFirstName,
			(SELECT * FROM dbo.BEMA_ReportingTools_ufn_GetFamilyTitleFormal(null, mpId.GroupId, (Select String_agg(p1.Id,',') From Person p1 where p1.GivingId = p.GivingId), 0)) 
				as GivingUnit_FullNameFirstName
	From Person p
	Join (Select distinct * From @ModifiedPersonIds) mpId on p.Id = mpId.PersonId
	Order By LastName desc, FirstName 
