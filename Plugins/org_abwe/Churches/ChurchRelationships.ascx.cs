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
using System.ComponentModel;
using System.Linq;
using System.Web.UI;
using System.Web.UI.WebControls;

using Rock;
using Rock.Attribute;
using Rock.Data;
using Rock.Model;
using Rock.Security;

namespace RockWeb.Plugins.org_abwe.RockMissions
{
    /// <summary>
    /// Renders the related members of a group (typically used for the Relationships group and the Peer Network group)
    /// </summary>
    [DisplayName( "Church Relationships" )]
    [Category( "org_abwe > RockMissions > Churches" )]
    [Description( "Allows you to view church relationships of a particular church." )]

    #region Block Attributes

    //[GroupRoleField(
    //    Name = "Group Type/Role Filter",
    //    Key = AttributeKey.GroupTypeRoleFilter,
    //    Description = "The Group Type and role to display other members from.",
    //    IsRequired = false,
    //    Order = 0 )]

    [BooleanField(
        "Show Role",
        Key = AttributeKey.ShowRole,
        Description = "Should the member's role be displayed with their name",
        Order = 1 )]

    [IntegerField(
        "Max Relationships To Display",
        Key = AttributeKey.MaxRelationshipsToDisplay,
        Description = "The maximum number of relationships to display.",
        IsRequired = false,
        DefaultIntegerValue = 50,
        Order = 3 )]

    [CustomCheckboxListField(
        "Roles to display",
        Description = "The roles from the group type to show in this list",
        Key = AttributeKey.Roles,
        ListSource = @"SELECT GroupTypeRole.[Id] [Value], GroupTypeRole.[Name] [Text]
            FROM[GroupTypeRole]
            INNER JOIN[GroupType] ON[GroupTypeRole].GroupTypeId = [GroupType].[Id]
            WHERE[GroupType].[Guid] = 'be7a89fa-4815-4258-a9c6-c08c058ee933'",
        RepeatDirection = RepeatDirection.Vertical
        )]

    #endregion Block Attributes

    public partial class ChurchRelationships : Rock.Web.UI.PersonBlock
    {
        #region Attribute Keys
        private static class AttributeKey
        {
            public const string ShowRole = "ShowRole";
            public const string Roles = "Roles";
            public const string MaxRelationshipsToDisplay = "MaxRelationshipsToDisplay";
        }
        #endregion Attribute Keys

        #region Properties

        /// <summary>
        /// Gets or sets a value indicating whether this instance can edit.
        /// </summary>
        /// <value>
        ///   <c>true</c> if this instance can edit; otherwise, <c>false</c>.
        /// </value>
        protected bool CanEdit { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether [show role].
        /// </summary>
        /// <value>
        ///   <c>true</c> if [show role]; otherwise, <c>false</c>.
        /// </value>
        protected bool ShowRole { get; set; }

        /// <summary>
        /// Gets or sets the owner role unique identifier.
        /// </summary>
        /// <value>
        /// The owner role unique identifier.
        /// </value>
        protected Guid ownerRoleGuid { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether this instance is inverse relationships owner.
        /// </summary>
        /// <value>
        /// <c>true</c> if this instance is inverse relationships owner; otherwise, <c>false</c>.
        /// </value>
        protected bool IsInverseRelationshipsOwner { get; set; }

        #endregion Properties

        #region Base Control Methods

        /// <summary>
        /// Raises the <see cref="E:System.Web.UI.Control.Init" /> event.
        /// </summary>
        /// <param name="e">An <see cref="T:System.EventArgs" /> object that contains the event data.</param>
        protected override void OnInit( EventArgs e )
        {
            base.OnInit( e );

            ownerRoleGuid = org.abwe.RockMissions.SystemGuid.GroupTypeRole.GROUPROLE_CHURCH.AsGuid();

            // The 'owner' of the group is determined by built-in KnownRelationshipsOwner role or the role that is marked as IsLeader for the group
            var ownerRole = new GroupTypeRoleService( new RockContext() ).Get( ownerRoleGuid );
            if ( ownerRole != null )
            {
                ownerRole.LoadAttributes();
                IsInverseRelationshipsOwner = true;
            }

            rGroupMembers.ItemDataBound += rGroupMembers_ItemDataBound;
            rGroupMembers.ItemCommand += rGroupMembers_ItemCommand;

            modalAddPerson.SaveClick += modalAddPerson_SaveClick;
            modalAddPerson.OnCancelScript = string.Format( "$('#{0}').val('');", hfRoleId.ClientID );

            CanEdit = IsUserAuthorized( Authorization.EDIT );
            lbAdd.Visible = CanEdit;

            string script = @"
    $('a.remove-relationship').on('click', function(){
        return confirm('Are you sure you want to remove this relationship?');
    });
";
            ScriptManager.RegisterStartupScript( rGroupMembers, rGroupMembers.GetType(), "ConfirmRemoveRelationship", script, true );
        }

        /// <summary>
        /// Raises the <see cref="E:System.Web.UI.Control.Load" /> event.
        /// </summary>
        /// <param name="e">The <see cref="T:System.EventArgs" /> object that contains the event data.</param>
        protected override void OnLoad( EventArgs e )
        {
            base.OnLoad( e );

            if ( Person != null && Person.Id != 0 )
            {
                upRelationships.Visible = true;

                ShowRole = GetAttributeValue( AttributeKey.ShowRole ).AsBoolean();

                if ( !Page.IsPostBack )
                {
                    BindData();
                }
                else
                {
                    ShowDialog();
                }
            }
            else
            {
                upRelationships.Visible = false;
            }
        }

        #endregion Base Control Methods

        #region Events

        /// <summary>
        /// Handles the Click event of the lbAdd control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        protected void lbAdd_Click( object sender, EventArgs e )
        {
            if ( CanEdit )
            {
                ShowModal( null, null, null );
            }
        }

        /// <summary>
        /// Handles the ItemDataBound event of the rGroupMembers control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="System.Web.UI.WebControls.RepeaterItemEventArgs"/> instance containing the event data.</param>
        protected void rGroupMembers_ItemDataBound( object sender, System.Web.UI.WebControls.RepeaterItemEventArgs e )
        {
            var lbEdit = e.Item.FindControl( "lbEdit" ) as LinkButton;
            var lbRemove = e.Item.FindControl( "lbRemove" ) as LinkButton;

            if ( lbEdit != null && lbRemove != null )
            {
                lbEdit.Visible = CanEdit;
                lbRemove.Visible = CanEdit;
            }
        }

        /// <summary>
        /// Handles the ItemCommand event of the rGroupMembers control.
        /// </summary>
        /// <param name="source">The source of the event.</param>
        /// <param name="e">The <see cref="System.Web.UI.WebControls.RepeaterCommandEventArgs"/> instance containing the event data.</param>
        protected void rGroupMembers_ItemCommand( object source, System.Web.UI.WebControls.RepeaterCommandEventArgs e )
        {
            if ( CanEdit )
            {
                int groupMemberId = e.CommandArgument.ToString().AsIntegerOrNull() ?? 0;
                if ( groupMemberId != 0 )
                {
                    var rockContext = new RockContext();
                    var service = new GroupMemberService( rockContext );
                    var groupMember = service.Get( groupMemberId );
                    if ( groupMember != null )
                    {
                        if ( e.CommandName == "EditRole" )
                        {
                            ShowModal( groupMember.Person, groupMember.GroupRoleId, groupMemberId );
                        }
                        else if ( e.CommandName == "RemoveRole" )
                        {
                            if ( IsInverseRelationshipsOwner )
                            {
                                var inverseGroupMember = service.GetInverseRelationship( groupMember, false );
                                if ( inverseGroupMember != null )
                                {
                                    service.Delete( inverseGroupMember );
                                }
                            }

                            service.Delete( groupMember );

                            rockContext.SaveChanges();

                            BindData();
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Handles the SaveClick event of the modalAddPerson control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        protected void modalAddPerson_SaveClick( object sender, EventArgs e )
        {
            if ( CanEdit )
            {
                if ( ppPerson.PersonId.HasValue )
                {
                    int? roleId = grpRole.GroupRoleId;
                    if ( roleId.HasValue )
                    {
                        var rockContext = new RockContext();
                        var memberService = new GroupMemberService( rockContext );

                        var group = memberService.Queryable( true )
                            .Where( m =>
                                m.PersonId == Person.Id &&
                                m.GroupRole.Guid == ownerRoleGuid )
                            .Select( m => m.Group )
                            .FirstOrDefault();

                        if ( group != null )
                        {
                            GroupMember groupMember = null;
                            int? groupMemberId = hfRoleId.Value.AsIntegerOrNull();
                            if ( groupMemberId.HasValue )
                            {
                                groupMember = memberService.Queryable( true )
                                .Where( m => m.Id == groupMemberId.Value )
                                .FirstOrDefault();
                            }

                            if ( groupMember == null )
                            {
                                groupMember = new GroupMember();
                                groupMember.GroupId = group.Id;
                                memberService.Add( groupMember );
                            }

                            GroupMember formerInverseGroupMember = null;
                            if ( IsInverseRelationshipsOwner )
                            {
                                formerInverseGroupMember = memberService.GetInverseRelationship( groupMember, false );
                            }

                            groupMember.PersonId = ppPerson.PersonId.Value;
                            groupMember.GroupRoleId = roleId.Value;

                            rockContext.SaveChanges();

                            if ( IsInverseRelationshipsOwner )
                            {
                                var inverseGroupMember = memberService.GetInverseRelationship( groupMember, false );
                                if ( inverseGroupMember != null )
                                {
                                    rockContext.SaveChanges();
                                    if ( formerInverseGroupMember != null && formerInverseGroupMember.Id != inverseGroupMember.Id )
                                    {
                                        memberService.Delete( formerInverseGroupMember );
                                        rockContext.SaveChanges();
                                    }
                                }
                            }
                        }
                    }
                }

                HideDialog();

                BindData();
            }
        }

        #endregion Events

        #region Methods

        /// <summary>
        /// Binds the data.
        /// </summary>
        private void BindData()
        {
            if ( Person != null && Person.Id > 0 )
            {
                if ( ownerRoleGuid != Guid.Empty )
                {
                    using ( var rockContext = new RockContext() )
                    {
                        var memberService = new GroupMemberService( rockContext );
                        var group = memberService.Queryable( true )
                            .Where( m =>
                                m.PersonId == Person.Id &&
                                m.GroupRole.Guid == ownerRoleGuid )
                            .Select( m => m.Group )
                            .FirstOrDefault();

                        if ( group != null )
                        {
                            //lGroupName.Text = group.Name.Pluralize();
                            lGroupTypeIcon.Text = string.Format( "<i class='{0}'></i>", group.GroupType.IconCssClass );
                            lGroupTypeIcon.Visible = group.GroupType.IconCssClass.IsNotNullOrWhiteSpace();
                            phEditActions.Visible = group.IsAuthorized( Authorization.EDIT, CurrentPerson );

                            if ( group.IsAuthorized( Authorization.VIEW, CurrentPerson ) )
                            {
                                int? maxRelationshipsToDisplay = this.GetAttributeValue( AttributeKey.MaxRelationshipsToDisplay ).AsIntegerOrNull();

                                var roleIdsToDisplay = GetAttributeValue(AttributeKey.Roles).StringToIntList();
                                IQueryable<GroupMember> qryGroupMembers = new GroupMemberService( rockContext ).GetByGroupId( group.Id, true )
                                    .Where( m => m.PersonId != Person.Id )
                                    .Where( m => roleIdsToDisplay.Any(id => id == m.GroupRoleId))
                                    .OrderBy( m => m.Person.LastName )
                                    .ThenBy( m => m.Person.FirstName );

                                if ( maxRelationshipsToDisplay.HasValue )
                                {
                                    qryGroupMembers = qryGroupMembers.Take( maxRelationshipsToDisplay.Value );
                                }

                                rGroupMembers.DataSource = qryGroupMembers.ToList();
                                rGroupMembers.DataBind();
                            }
                            else
                            {
                                lAccessWarning.Text = string.Format( "<div class='alert alert-info'>You do not have security rights to view {0}.", group.Name.Pluralize() );
                            }
                        }
                        else
                        {
                            //lGroupName.Text = this.BlockCache.Name;
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Shows the modal.
        /// </summary>
        /// <param name="person">The person.</param>
        /// <param name="roleId">The role identifier.</param>
        /// <param name="groupMemberId">The group member identifier.</param>
        private void ShowModal( Person person, int? roleId, int? groupMemberId )
        {
            Guid roleGuid = org.abwe.RockMissions.SystemGuid.GroupTypeRole.GROUPROLE_CHURCH.AsGuid();
            if ( roleGuid != Guid.Empty )
            {
                var groupTypeRoleService = new GroupTypeRoleService( new RockContext() );
                var role = groupTypeRoleService.Get( ownerRoleGuid );
                grpRole.ExcludeGroupRoles.Add( role.Id );

                grpRole.GroupTypeId = groupTypeRoleService.Queryable()
                    .Where( r => r.Guid == roleGuid )
                    .Select( r => r.GroupTypeId )
                    .FirstOrDefault();
            }

            grpRole.GroupRoleId = roleId;
            ppPerson.SetValue( person );

            ShowDialog( groupMemberId ?? 0, true );
        }

        /// <summary>
        /// Shows the dialog.
        /// </summary>
        /// <param name="roleId">The role identifier.</param>
        /// <param name="setValues">if set to <c>true</c> [set values].</param>
        private void ShowDialog( int roleId, bool setValues = false )
        {
            hfRoleId.Value = roleId.ToString();
            ShowDialog( setValues );
        }

        /// <summary>
        /// Shows the dialog.
        /// </summary>
        /// <param name="setValues">if set to <c>true</c> [set values].</param>
        private void ShowDialog( bool setValues = false )
        {
            if ( !string.IsNullOrWhiteSpace( hfRoleId.Value ) )
            {
                modalAddPerson.Show();
            }
        }

        /// <summary>
        /// Hides the dialog.
        /// </summary>
        private void HideDialog()
        {
            modalAddPerson.Hide();
            hfRoleId.Value = string.Empty;
        }

        #endregion Methods

    }
}