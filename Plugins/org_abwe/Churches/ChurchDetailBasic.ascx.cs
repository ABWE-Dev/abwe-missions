// <copyright>
// Copyright by ABWE
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
//
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Web.UI;
using System.Web.UI.WebControls;

using Rock;
using Rock.Attribute;
using Rock.Constants;
using Rock.Data;
using Rock.Model;
using Rock.Security;
using Rock.Web;
using Rock.Web.Cache;
using Rock.Web.UI;
using Rock.Web.UI.Controls;

namespace RockWeb.Plugins.org_abwe.RockMissions
{
    [DisplayName( "Church Detail Basic" )]
    [Category( "org_abwe > RockMissions > Churches" )]
    [Description( "Displays the details of the given church. Differents from the church detail block in not showing group members in this block." )]

    [LinkedPage( "Person Profile Page", "The page used to view the details of a church contact", order: 0 )]
    [LinkedPage( "Communication Page", "The communication page to use for when the church email address is clicked. Leave this blank to use the default.", false, "", "", 1 )]
    public partial class ChurchDetail : Rock.Web.UI.RockBlock, IDetailBlock
    {
        #region Base Control Methods

        /// <summary>
        /// Raises the <see cref="E:System.Web.UI.Control.Init" /> event.
        /// </summary>
        /// <param name="e">An <see cref="T:System.EventArgs" /> object that contains the event data.</param>
        protected override void OnInit( EventArgs e )
        {
            base.OnInit( e );

            dvpRecordStatus.DefinedTypeId = DefinedTypeCache.Get( new Guid( Rock.SystemGuid.DefinedType.PERSON_RECORD_STATUS ) ).Id;
            dvpReason.DefinedTypeId = DefinedTypeCache.Get( new Guid( Rock.SystemGuid.DefinedType.PERSON_RECORD_STATUS_REASON ) ).Id;

            bool canEdit = IsUserAuthorized( Authorization.EDIT );
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
                int? ChurchId = PageParameter("ChurchId").AsIntegerOrNull();
                int? ChurchGroupId = PageParameter("GroupId").AsIntegerOrNull();

                // If there's a PersonId, we should consider it a church id
                if (ChurchId == null)
                {
                    int? PersonId = PageParameter("PersonId").AsIntegerOrNull();
                    if (PersonId.HasValue)
                    {
                        var pageReference = RockPage.PageReference;
                        pageReference.Parameters.AddOrReplace("ChurchId", PersonId.ToString());
                        pageReference.Parameters.Remove("GroupId");
                        Response.Redirect(pageReference.BuildUrl(), true);
                    }
                }

                // If there is not a church ID (which is a person ID) provided,
                // check to see if there is a group ID for a church group provided and find the church
                // in that church group. Allows us to use a GroupList block to link to a church page
                if (ChurchId == null)
                {
                    if (ChurchGroupId == null)
                    {
                        nbErrorMessage.Text = "No church found.";
                        SetEditMode(true);
                        pnlEditDetails.Visible = false;
                        return;
                    }
                    
                    Guid ChurchRoleGuid = org.abwe.RockMissions.SystemGuid.GroupTypeRole.GROUPROLE_CHURCH.AsGuid();
                    ChurchId = new GroupMemberService(new RockContext()).Queryable()
                            .Where(gm => gm.GroupId == ChurchGroupId && gm.GroupRole.Guid == ChurchRoleGuid)
                            .Select(gm => gm.PersonId)
                            .FirstOrDefault();

                    if (ChurchId == 0)
                    {
                        // TODO: Show an error if the group is not a church
                        nbErrorMessage.Text = "This is not a church group";
                        SetEditMode(true);
                        pnlEditDetails.Visible = false;
                        return;
                    }

                    var pageReference = RockPage.PageReference;
                    pageReference.Parameters.AddOrReplace("ChurchId", ChurchId.ToString());
                    Response.Redirect(pageReference.BuildUrl(), false);
                }

                // If the GroupId Page Parameter is not set, set it so that
                // we can use a GroupMemberList block on the page to display the members
                if (ChurchGroupId == null)
                {
                    Guid ChurchRoleGuid = org.abwe.RockMissions.SystemGuid.GroupTypeRole.GROUPROLE_CHURCH.AsGuid();
                    ChurchGroupId = new GroupMemberService(new RockContext()).Queryable()
                            .Where(gm => gm.PersonId == ChurchId && gm.GroupRole.Guid == ChurchRoleGuid)
                            .Select(gm => gm.GroupId)
                            .FirstOrDefault();

                    if (ChurchGroupId != null)
                    {
                        var pageReference = RockPage.PageReference;
                        pageReference.Parameters.AddOrReplace("ChurchId", ChurchId.ToString());
                        pageReference.Parameters.AddOrReplace("GroupId", ChurchGroupId.ToString());
                        Response.Redirect(pageReference.BuildUrl(), false);
                    }
                }

                ShowDetail(ChurchId.Value);
            }

            if (!IsUserAuthorized(Authorization.EDIT))
            {
                lbEdit.Enabled = false;
            }
            
        }

        #endregion Control Methods

        #region Events

        /// <summary>
        /// Handles the Click event of the lbEdit control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        protected void lbEdit_Click( object sender, EventArgs e )
        {
            var rockContext = new RockContext();
            var church = new PersonService( rockContext ).Get( int.Parse( hfChurchId.Value ) );
            ShowEditDetails( church );
        }

        /// <summary>
        /// Handles the Click event of the lbSave control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        protected void lbSave_Click( object sender, EventArgs e )
        {
            var rockContext = new RockContext();

            var personService = new PersonService( rockContext );
            Person church = null;

            if ( int.Parse( hfChurchId.Value ) != 0 )
            {
                church = personService.Get( int.Parse( hfChurchId.Value ) );
            }

            if ( church == null )
            {
                church = new Person();
                personService.Add( church );
                tbChurchName.Text = tbChurchName.Text.FixCase();
            }

            // Church Name
            church.LastName = tbChurchName.Text;

            // Phone Number
            var churchPhoneTypeId = new DefinedValueService( rockContext ).GetByGuid( new Guid( Rock.SystemGuid.DefinedValue.PERSON_PHONE_TYPE_WORK ) ).Id;

            var phoneNumber = church.PhoneNumbers.FirstOrDefault( n => n.NumberTypeValueId == churchPhoneTypeId );

            if ( !string.IsNullOrWhiteSpace( PhoneNumber.CleanNumber( pnbPhone.Number ) ) )
            {
                if ( phoneNumber == null )
                {
                    phoneNumber = new PhoneNumber { NumberTypeValueId = churchPhoneTypeId };
                    church.PhoneNumbers.Add( phoneNumber );
                }
                phoneNumber.CountryCode = PhoneNumber.CleanNumber( pnbPhone.CountryCode );
                phoneNumber.Number = PhoneNumber.CleanNumber( pnbPhone.Number );
                phoneNumber.IsMessagingEnabled = cbSms.Checked;
                phoneNumber.IsUnlisted = cbUnlisted.Checked;
            }
            else
            {
                if ( phoneNumber != null )
                {
                    church.PhoneNumbers.Remove( phoneNumber );
                    new PhoneNumberService( rockContext ).Delete( phoneNumber );
                }
            }

            // Record Type - this is always "church". it will never change.
            church.RecordTypeValueId = DefinedValueCache.Get(Rock.SystemGuid.DefinedValue.PERSON_RECORD_TYPE_BUSINESS).Id;

            // Record Status
            church.RecordStatusValueId = dvpRecordStatus.SelectedValueAsInt(); ;

            // Record Status Reason
            int? newRecordStatusReasonId = null;
            if ( church.RecordStatusValueId.HasValue && church.RecordStatusValueId.Value == DefinedValueCache.Get( new Guid( Rock.SystemGuid.DefinedValue.PERSON_RECORD_STATUS_INACTIVE ) ).Id )
            {
                newRecordStatusReasonId = dvpReason.SelectedValueAsInt();
            }
            church.RecordStatusReasonValueId = newRecordStatusReasonId;

            // Email
            church.IsEmailActive = true;
            church.Email = tbEmail.Text.Trim();
            church.EmailPreference = rblEmailPreference.SelectedValue.ConvertToEnum<EmailPreference>();

            if ( !church.IsValid )
            {
                // Controls will render the error messages
                return;
            }

            rockContext.WrapTransaction( () =>
            {
                rockContext.SaveChanges();

                // Add/Update Family Group
                var familyGroupType = GroupTypeCache.GetFamilyGroupType();
                int adultRoleId = familyGroupType.Roles
                    .Where( r => r.Guid.Equals( Rock.SystemGuid.GroupRole.GROUPROLE_FAMILY_MEMBER_ADULT.AsGuid() ) )
                    .Select( r => r.Id )
                    .FirstOrDefault();
                var adultFamilyMember = UpdateGroupMember( church.Id, familyGroupType, church.LastName + " Church", adultRoleId, rockContext );

                //church.GivingGroup = adultFamilyMember.Group;

                // Add/Update Church Group Type
                var churchGroupType = GroupTypeCache.Get(org.abwe.RockMissions.SystemGuid.GroupType.GROUPTYPE_CHURCH.AsGuid() );
                int churchGroupTypeRoleId = 0;
                if (churchGroupType != null)
                {
                    churchGroupTypeRoleId = churchGroupType.Roles
                        .Where(r => r.Guid == org.abwe.RockMissions.SystemGuid.GroupTypeRole.GROUPROLE_CHURCH.AsGuid())
                        .Select(a => a.Id).FirstOrDefault();
                }
                var churchGroupKey = UpdateGroupMember(church.Id, churchGroupType, church.LastName, churchGroupTypeRoleId, rockContext);
               
                rockContext.SaveChanges();

                // Location
                int workLocationTypeId = DefinedValueCache.Get( Rock.SystemGuid.DefinedValue.GROUP_LOCATION_TYPE_WORK ).Id;

                var groupLocationService = new GroupLocationService( rockContext );
                var workLocation = groupLocationService.Queryable( "Location" )
                    .Where( gl =>
                        gl.GroupId == adultFamilyMember.Group.Id &&
                        gl.GroupLocationTypeValueId == workLocationTypeId )
                    .FirstOrDefault();
                if (string.IsNullOrWhiteSpace( acAddress.Street1 ))
                {
                    acAddress.Street1 = "unknown";
                }

                if ( string.IsNullOrWhiteSpace( acAddress.City ) )
                {
                    if ( workLocation != null )
                    {
                        groupLocationService.Delete( workLocation );
                    }
                }
                else
                {
                    var newLocation = new LocationService( rockContext ).Get(
                        acAddress.Street1, acAddress.Street2, acAddress.City, acAddress.State, acAddress.PostalCode, acAddress.Country );
                    if ( workLocation == null )
                    {
                        workLocation = new GroupLocation();
                        groupLocationService.Add( workLocation );
                        workLocation.GroupId = adultFamilyMember.Group.Id;
                        workLocation.GroupLocationTypeValueId = workLocationTypeId;
                    }
                    workLocation.Location = newLocation;
                    workLocation.IsMailingLocation = true;
                }

                rockContext.SaveChanges();

                hfChurchId.Value = church.Id.ToString();
            } );

            var queryParams = new Dictionary<string, string>();
            queryParams.Add( "ChurchId", hfChurchId.Value );
            NavigateToCurrentPage( queryParams );
        }

        /// <summary>
        /// Handles the Click event of the lbCancel control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        protected void lbCancel_Click( object sender, EventArgs e )
        {
            int? churchId = hfChurchId.Value.AsIntegerOrNull();
            if ( churchId.HasValue && churchId > 0 )
            {
                ShowSummary( churchId.Value );
            }
            else
            {
                NavigateToParentPage();
            }
        }

        /// <summary>
        /// Handles the SelectedIndexChanged event of the ddlRecordStatus control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        protected void ddlRecordStatus_SelectedIndexChanged( object sender, EventArgs e )
        {
            dvpReason.Visible = dvpRecordStatus.SelectedValueAsInt() == DefinedValueCache.Get( new Guid( Rock.SystemGuid.DefinedValue.PERSON_RECORD_STATUS_INACTIVE ) ).Id;
        }

        #endregion Events

        #region Internal Methods

        /// <summary>
        /// Shows the detail.
        /// </summary>
        /// <param name="churchId">The church identifier.</param>
        public void ShowDetail( int churchId )
        {
            var rockContext = new RockContext();

            Person church = null;     // A church is a person

            if ( !churchId.Equals( 0 ) )
            {
                church = new PersonService( rockContext ).Get( churchId );
                pdAuditDetails.SetEntity( church, ResolveRockUrl( "~" ) );
            }

            if ( church == null )
            {
                church = new Person { Id = 0, Guid = Guid.NewGuid() };
                // hide the panel drawer that show created and last modified dates
                pdAuditDetails.Visible = false;
            }

            bool editAllowed = church.IsAuthorized( Authorization.EDIT, CurrentPerson );

            hfChurchId.Value = church.Id.ToString();

            bool readOnly = false;

            nbEditModeMessage.Text = string.Empty;
            if ( !editAllowed || !IsUserAuthorized( Authorization.EDIT ) )
            {
                readOnly = true;
                nbEditModeMessage.Text = EditModeMessage.ReadOnlyEditActionNotAllowed( Person.FriendlyTypeName );
            }

            if ( readOnly )
            {
                ShowSummary( churchId );
            }
            else
            {
                if ( church.Id > 0 )
                {
                    ShowSummary( church.Id );
                }
                else
                {
                    ShowEditDetails( church );
                }
            }

            //var churchGroupType = GroupTypeCache.Get(org.abwe.RockMissions.SystemGuid.GroupType.GROUPTYPE_CHURCH.AsGuid());
            //var roles = churchGroupType.Roles.OrderBy (a => a.Order).ToList();
            //var itemToRemove = roles.Single(r => r.Name == "Church"); 
            //if (itemToRemove != null)
            //{
            //    roles.Remove(itemToRemove);
            //}
        }

        /// <summary>
        /// Shows the summary.
        /// </summary>
        /// <param name="church">The church.</param>
        private void ShowSummary( int churchId )
        {
            SetEditMode( false );
            hfChurchId.SetValue( churchId );
            lTitle.Text = "View Church".FormatAsHtmlTitle();

            var church = new PersonService( new RockContext() ).Get( churchId );
            if ( church != null )
            {
                SetHeadingStatusInfo( church );
                var detailsLeft = new DescriptionList();
                detailsLeft.Add( "Church Name", church.LastName );

                if ( church.RecordStatusReasonValue != null )
                {
                    detailsLeft.Add( "Record Status Reason", church.RecordStatusReasonValue );
                }

                detailsLeft.Add("SE Account #", $"<a target='_blank' href='https://seweb.abwe.org/AT/accounts/{church.ForeignId}'>{church.ForeignId}</a>");

                lDetailsLeft.Text = detailsLeft.Html;

                var detailsRight = new DescriptionList();

                // Get address
                var workLocationType = DefinedValueCache.Get( Rock.SystemGuid.DefinedValue.GROUP_LOCATION_TYPE_WORK.AsGuid() );
                if ( workLocationType != null )
                {
                    if ( church.PrimaryFamily != null ) // Giving Group is a shortcut to Family Group for church
                    {
                        var location = church.PrimaryFamily.GroupLocations
                            .Where( gl => gl.GroupLocationTypeValueId == workLocationType.Id )
                            .Select( gl => gl.Location )
                            .FirstOrDefault();
                        if ( location != null )
                        {
                            detailsRight.Add( "Address", location.GetFullStreetAddress().ConvertCrLfToHtmlBr() );
                        }
                    }
                }

                var workPhoneType = DefinedValueCache.Get( Rock.SystemGuid.DefinedValue.PERSON_PHONE_TYPE_WORK.AsGuid() );
                if ( workPhoneType != null )
                {
                    var phoneNumber = church.PhoneNumbers.FirstOrDefault( n => n.NumberTypeValueId == workPhoneType.Id );
                    if ( phoneNumber != null )
                    {
                        detailsRight.Add( "Phone Number", phoneNumber.ToString() );
                    }
                }

                var communicationLinkedPageValue = this.GetAttributeValue( "CommunicationPage" );
                Rock.Web.PageReference communicationPageReference;
                if ( communicationLinkedPageValue.IsNotNullOrWhiteSpace() )
                {
                    communicationPageReference = new Rock.Web.PageReference( communicationLinkedPageValue );
                }
                else
                {
                    communicationPageReference = null;
                }

                detailsRight.Add( "Email Address", church.GetEmailTag( ResolveRockUrl( "/" ), communicationPageReference ) );

                lDetailsRight.Text = detailsRight.Html;
            }
        }

        /// <summary>
        /// Sets the heading Status information.
        /// </summary>
        /// <param name="church">The church.</param>
        private void SetHeadingStatusInfo( Person church )
        {
            if ( church.RecordStatusValue != null )
            {
                hlStatus.Text = church.RecordStatusValue.Value;
                if ( church.RecordStatusValue.Guid == Rock.SystemGuid.DefinedValue.PERSON_RECORD_STATUS_PENDING.AsGuid() )
                {
                    hlStatus.LabelType = LabelType.Warning;
                }
                else if ( church.RecordStatusValue.Guid == Rock.SystemGuid.DefinedValue.PERSON_RECORD_STATUS_INACTIVE.AsGuid() )
                {
                    hlStatus.LabelType = LabelType.Danger;
                }
                else
                {
                    hlStatus.LabelType = LabelType.Success;
                }
            }
        }

        /// <summary>
        /// Shows the edit details.
        /// </summary>
        /// <param name="church">The church.</param>
        private void ShowEditDetails( Person church )
        {
            if ( church.Id > 0 )
            {
                var rockContext = new RockContext();

                lTitle.Text = ActionTitle.Edit( church.FullName ).FormatAsHtmlTitle();
                tbChurchName.Text = church.LastName;

                // address
                Location location = null;
                var workLocationType = DefinedValueCache.Get( Rock.SystemGuid.DefinedValue.GROUP_LOCATION_TYPE_WORK.AsGuid() );
                if ( church.PrimaryFamily != null )     // Giving group is a shortcut to the family group for church
                {
                    location = church.PrimaryFamily.GroupLocations
                        .Where( gl => gl.GroupLocationTypeValueId == workLocationType.Id )
                        .Select( gl => gl.Location )
                        .FirstOrDefault();
                }
                acAddress.SetValues( location );

                // Phone Number
                var workPhoneType = DefinedValueCache.Get( Rock.SystemGuid.DefinedValue.PERSON_PHONE_TYPE_WORK.AsGuid() );
                PhoneNumber phoneNumber = null;
                if ( workPhoneType != null )
                {
                    phoneNumber = church.PhoneNumbers.FirstOrDefault( n => n.NumberTypeValueId == workPhoneType.Id );
                }
                if ( phoneNumber != null )
                {
                    pnbPhone.Text = phoneNumber.NumberFormatted;
                    cbSms.Checked = phoneNumber.IsMessagingEnabled;
                    cbUnlisted.Checked = phoneNumber.IsUnlisted;
                }
                else
                {
                    pnbPhone.Text = string.Empty;
                    cbSms.Checked = false;
                    cbUnlisted.Checked = false;
                }

                tbEmail.Text = church.Email;
                rblEmailPreference.SelectedValue = church.EmailPreference.ToString();

                dvpRecordStatus.SelectedValue = church.RecordStatusValueId.HasValue ? church.RecordStatusValueId.Value.ToString() : string.Empty;
                dvpReason.SelectedValue = church.RecordStatusReasonValueId.HasValue ? church.RecordStatusReasonValueId.Value.ToString() : string.Empty;
                dvpReason.Visible = church.RecordStatusReasonValueId.HasValue &&
                    church.RecordStatusValueId.Value == DefinedValueCache.Get( new Guid( Rock.SystemGuid.DefinedValue.PERSON_RECORD_STATUS_INACTIVE ) ).Id;
            }
            else
            {
                lTitle.Text = ActionTitle.Add( "Church" ).FormatAsHtmlTitle();
            }

            SetEditMode( true );
        }

        /// <summary>
        /// Sets the edit mode.
        /// </summary>
        /// <param name="editable">if set to <c>true</c> [editable].</param>
        private void SetEditMode( bool editable )
        {
            pnlEditDetails.Visible = editable;
            fieldsetViewSummary.Visible = !editable;
            this.HideSecondaryBlocks( editable );
        }

        /// <summary>
        /// Updates the group member.
        /// </summary>
        /// <param name="churchId">The church identifier.</param>
        /// <param name="groupType">Type of the group.</param>
        /// <param name="groupName">Name of the group.</param>
        /// <param name="groupRoleId">The group role identifier.</param>
        /// <param name="rockContext">The rock context.</param>
        /// <returns></returns>
        private GroupMember UpdateGroupMember( int churchId, GroupTypeCache groupType, string groupName, int groupRoleId, RockContext rockContext )
        {
            var groupMemberService = new GroupMemberService( rockContext );

            GroupMember groupMember = groupMemberService.Queryable( "Group" )
                .Where( m =>
                    m.PersonId == churchId &&
                    m.GroupRoleId == groupRoleId )
                .FirstOrDefault();

            if ( groupMember == null )
            {
                groupMember = new GroupMember();
                groupMember.Group = new Group();
            }

            groupMember.PersonId = churchId;
            groupMember.GroupRoleId = groupRoleId;
            groupMember.GroupMemberStatus = GroupMemberStatus.Active;

            groupMember.Group.GroupTypeId = groupType.Id;
            groupMember.Group.Name = groupName;

            if ( groupMember.Id == 0)
            {
                groupMemberService.Add( groupMember );
            }

            return groupMember;
        }

        #endregion Internal Methods

    }
}
