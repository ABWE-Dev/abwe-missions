<%@ Control Language="C#" AutoEventWireup="true" CodeFile="StepsTimeline.ascx.cs" Inherits="RockWeb.Plugins.org_abwe.RockMissions.StepsTimeline" %>

<style>
.panel-fullwidth {
    margin: -15px -15px 0 -15px;
}

.swimlanes .grid-background {
  fill: none; }

.swimlanes .grid-header {
  fill: #f7f7f7;
  stroke: #e5e5e5;
  stroke-width: .3; }

.swimlanes .grid-row {
  fill: #fff; }

.swimlanes .grid-row:nth-child(even) {
  fill: #f9f9f9; }

.swimlanes .row-line {
  stroke: #e5e5e5; }

.swimlanes .tick {
  stroke: #939393;
  stroke-width: .2; }
  .swimlanes .tick.thick {
    stroke-width: .4; }

.swimlanes .today-highlight {
  opacity: .5;
  fill: #fcf8e3; }

.swimlanes .bar {
  user-select: none;
  transition: stroke-width .3s ease;
  fill: #b8c2cc;
  stroke: #8d99a6;
  stroke-width: 0;
  opacity: 0.8;
}

.swimlanes .bar-invalid {
  fill: transparent;
  stroke: #8d99a6;
  stroke-width: 1;
  stroke-dasharray: 5; }
  .swimlanes .bar-invalid ~ .bar-label {
    fill: white; }

.swimlanes .bar-label, .swimlanes .left-for-field {
  font-size: 12px;
  font-weight: 400;
  fill: white;
  dominant-baseline: central;
  text-anchor: middle; }
  .swimlanes .bar-label.big {
    fill: #555;
    text-anchor: start; }

.swimlanes .left-for-field {
    font-weight: 900;
    stroke: black;
    stroke-width: 1px;
    font-size: 20px;
}

.swimlanes .bar-wrapper {
  cursor: pointer; }
  .swimlanes .bar-wrapper:hover .bar, .swimlanes .bar-wrapper.active .bar {
    stroke-width: 2;
    opacity: .8; }
  .swimlanes .bar-wrapper.is-leader .bar {
    opacity: 1; }

.swimlanes .lower-text,
.swimlanes .upper-text {
  font-size: 14px;
  text-anchor: middle; }

.swimlanes .upper-text {
  font-weight: 800;
  fill: #555; }

.swimlanes .lower-text {
  font-size: 12px;
  fill: #333; }

.swimlanes .hide {
  display: none; }

.gantt-container {
  position: relative;
  overflow: scroll;
  font-size: 12px; }
  .gantt-container .popup-wrapper {
    position: absolute;
    top: 0;
    left: 0;
    padding: 0;
    color: #959da5;
    background: rgba(0, 0, 0, 0.8);
    border-radius: 3px; }
    .gantt-container .popup-wrapper .title {
      padding: 10px;
      border-bottom: 1px solid #fff; }
      .gantt-container .popup-wrapper .title a {
        color: #fff;
        font-size: 16px; }
    .gantt-container .popup-wrapper .subtitle {
      padding: 10px;
      color: #dfe2e5; }
    .gantt-container .popup-wrapper .pointer {
      position: absolute;
      height: 5px;
      margin: 0 0 0 -5px;
      border: 5px solid transparent;
      border-top-color: rgba(0, 0, 0, 0.8); }

</style>

<script src="<%=this.RockPage.ResolveRockUrl("/Plugins/org_abwe/RockMissions/Scripts/frappe-gantt.min.js", true) %>"></script>

<asp:UpdatePanel ID="upnlContent" runat="server">
    <ContentTemplate>
        <asp:HiddenField ID="hfPersonId" runat="server" />
        <asp:HiddenField ID="hfPersonAliases" runat="server" />
        <asp:HiddenField ID="hfStepTypeIds" runat="server" />
        <asp:HiddenField ID="hfStepDetailsUrl" runat="server" />

        <div class="panel panel-block">
            <div class="panel-heading">
                <h1 class="panel-title">
                    <i class="fa fa-history"></i>
                    Timelines
                </h1>

                <a class="btn btn-xs btn-default pull-right margin-l-sm" onclick="javascript: toggleOptions()">
                    <i title="Options" class="fa fa-gear"></i>
                </a>

                <div class="btn-group btn-group-xs margin-l-sm hidden-xs" role="group" aria-label="...">
                    <button type="button" class="btn btn-default" onclick="gantt.change_view_mode('Week')">Week</button>
                    <button type="button" class="btn btn-default" onclick="gantt.change_view_mode('Month')">Month</button>
                    <button type="button" class="btn btn-default" onclick="gantt.change_view_mode('Year')">Year</button>
                    <button type="button" class="btn btn-default" onclick="gantt.change_view_mode('Decade')">Decade</button>
                </div>

                <!-- Single button -->
                <asp:Label ID="btnAdd" runat="server">
                <div class="btn-group">
                  <button type="button" class="btn btn-default dropdown-toggle btn-xs margin-l-sm" data-toggle="dropdown" aria-haspopup="true" aria-expanded="false">
                    <i class="fa fa-plus"></i> <span class="caret"></span>
                  </button>
                    
                  <ul class="dropdown-menu dropdown-menu-right">
                      <asp:Repeater runat="server" ID="ddAddStep">
                          <ItemTemplate>
                              <li><a href="#" onclick="__doPostBack('LoadStepDetails','<%# Eval("Id") %>,0')"><%# Eval("Name") %></a></li>
                          </ItemTemplate>
                      </asp:Repeater>
                  </ul>
                </div>
                </asp:Label>
            </div>

            <asp:Panel ID="pnlOptions" runat="server" Title="Options" CssClass="panel-body js-options" Style="display: none">
                <div class="row">
                    <div class="col-md-6">
                        <Rock:RockCheckBoxList ID="cblStepTypes" Label="Types" runat="server" />
                    </div>
                    <div class="col-md-6">
                    </div>
                </div>

                <div class="actions">
                    <asp:LinkButton ID="btnApplyOptions" runat="server" Text="Apply" CssClass="btn btn-primary btn-xs" OnClick="btnApplyOptions_Click" />
                </div>
            </asp:Panel>

            <div class="panel-body">
                <div class="js-no-group-history" style="display:none">
                    <Rock:NotificationBox ID="nbNoGroupHistoryFound" runat="server" NotificationBoxType="Info" Text="No Group History Available" />
                </div>
                 <asp:Panel ID="groupHistorySwimlanes" CssClass="panel-fullwidth" runat="server" />

                <div class="grouptype-legend">
                    <label>Timelines</label>
                    <div class="grouptype-legend">
                        <asp:Repeater ID="rptStepTypeLegend" runat="server" OnItemDataBound="rptStepTypeLegend_ItemDataBound">
                            <ItemTemplate>
                                <span class="padding-r-sm">
                                    <asp:Literal ID="lStepTypeBadgeHtml" runat="server" /></span>
                            </ItemTemplate>
                        </asp:Repeater>
                    </div>
                    <asp:Literal ID="lStepTimeLegend" runat="server" />
                </div>
            </div>
        </div>

        <script type="text/javascript">
            var currentMousePos = { x: -1, y: -1 };

            function toggleOptions() {
                $('.js-options').slideToggle();
            }

            function getStepTitle(step) {
                switch (step.data.StepType.Guid) {
                    case 'ff3e7f7f-4127-4a95-8990-2eecf2cc7c03': // Field Assignment
                        return `${step.data.AttributeValues["Details"][0].ValueFormatted}`;

                    case '7aae4cbb-9058-4beb-968b-4c0d9c92b4ef': // Appointment
                        return `${step.data.AttributeValues["Commitment"][0].ValueFormatted} ${step.data.AttributeValues["Length"][0].ValueFormatted} ${step.data.AttributeValues["Associate"][0].Value == "True" ? "Associate" : ""}`;

                    case '2b683892-c473-4d19-b847-2410881afc86': // Award
                        return `${step.data.AttributeValues["Type"][0].ValueFormatted}`;

                    case '2dabbcc8-19e6-4b59-aba2-a940cb876859': // Employment
                        return `Employment: ${step.data.AttributeValues["JobTitle"][0].ValueFormatted}`

                    case '7d10fe63-8df4-454c-92ca-b745708192b6': // Course
                        return `${step.data.AttributeValues["Course"][0].ValueFormatted}`;

                    default:
                        return `${step.data.StepType.Name}`;
                }
            }

            Sys.Application.add_load(function () {

                var restUrl = '<%=ResolveUrl( "~/api/Lava/RenderTemplate" ) %>'

                var filterParams = [];
                var personAliasIds = $('#<%=hfPersonAliases.ClientID%>').val();
                var personId = $('#<%=hfPersonId.ClientID%>').val();
                var stepTypeIds = $('#<%=hfStepTypeIds.ClientID%>').val();

                //filterParams.push(personAliasIds);

                //if (stepTypeIds && stepTypeIds != '') {
                //    var stepIdFilters = stepTypeIds.split(',').map((stepId) => `StepTypeId eq ${stepId}`);
                //    filterParams.push('(' + stepIdFilters.join(' or ') + ')');
                //}

                var $swimlanesContainer = $('#<%=groupHistorySwimlanes.ClientID%>');
                var $noGroupHistory = $('.js-no-group-history');

                $swimlanesContainer.append("<svg id='gantt' class='swimlanes'></svg>");

                $.ajax({
                    method: "POST",
                    data: `
                        {%- step expression:'PersonAlias.PersonId == ${personId}' -%}
                        {%- capture response -%}
                        [{%- for step in stepItems -%}
                            {
                                "id": "{{ step.Id }}",
                                "start": "{{ step.StartDateTime | Date:'yyyy-MM-ddTHH:mm:ss' }}",
                                "end": "{{ step.EndDateTime | Date:'yyyy-MM-ddTHH:mm:ss' }}",
                                "completed": "{{ step.CompletedDateTime | Date:'yyyy-MM-ddTHH:mm:ss' }}",
                                "Color": "{{ step.StepType.HighlightColor }}",
                                "data": {
                                    "StepType": {
                                        "Guid": "{{ step.StepType.Guid }}",
                                        "Id": "{{ step.StepTypeId }}",
                                        "Name" : "{{ step.StepType.Name }}"
                                    },
                                    "StepTypeId": "{{ step.StepTypeId }}",
                                    "AttributeValues": {{ step.AttributeValues | GroupBy:'AttributeKey' | ToJSON }},
                                    "Attributes" : { {% for av in step.AttributeValues %}
                                        "{{ av.AttributeKey }}" : { "Name" : "{{ av.AttributeName }}" }{% if forloop.last == false %},{% endif %}
                                    {% endfor %}
                                    },
                                    "Id" : "{{ step.Id }}"
                                }
                            }
                            {% if forloop.last == false %},{% endif %}
                            {%- endfor -%}]
                        {%- endcapture -%}
                        {%- endstep -%}
                        {{- response -}}
                    `,
                    url: restUrl,
                    dataType: 'json',
                    contentType: 'application/json'
                }).done(function (data) {
                    var parsedData = JSON.parse(data);
                    var steps = parsedData.filter(step => stepTypeIds.split(',').some(stepTypeId => stepTypeId == step.data.StepType.Id)).map(step => ({
                            id: step.Id,
                            name: getStepTitle(step),
                            start: step.start,
                            end: step.end || step.completed || null,
                            Color: step.Color,
                            data: {
                                ...step.data,
                                AttributeValues: Object.keys(step.data.AttributeValues).reduce((acc, current) => {
                                    acc[current] = step.data.AttributeValues[current][0];
                                    return acc;
                                }, {})
                            }
                        })
                    );

                    window.gantt = new Gantt("#gantt", steps, {
                        on_click: function (task) {
                            __doPostBack('LoadStepDetails', task.data.StepType.Id + ',' + task.data.Id);
                        },
                        view_mode: 'Year',
                        custom_popup_html: function (task) {
                            let attributeHtml = '<table style="background: initial;">';
                            for (let attributeValue in task.data.AttributeValues) {
                                let value = task.data.AttributeValues[attributeValue].ValueFormatted;
                                if (value != null && value != 'null') {
                                    attributeHtml += '<tr><td style="white-space: nowrap;padding-right: 10px;">'+task.data.Attributes[attributeValue].Name + ':</td><td style="white-space: nowrap;">' + value + '</td></tr>';
                                }
                            }
                            attributeHtml+='</table>'

                            let startDate = new Date(task.start);
                            let endDate = new Date(task.end);

                            let startDateFormat = startDate.getMonth() + 1 + '/' + startDate.getDate() + '/' + startDate.getFullYear();
                            let endDateFormat;
                            if (task.end == null) {
                                endDateFormat = "present"
                            } else {
                                endDateFormat = endDate.getMonth() + 1 + '/' + endDate.getDate() + '/' + endDate.getFullYear();
                            }

                            return `
                                <div class="title">${task.name}</div>
                                <div class="subtitle">${startDateFormat} - ${endDateFormat}</div>
                                <div class="subtitle">${attributeHtml}</div>
                            `;
                        }
                    });
                });


                $(document).mousemove(function(e) {
                    currentMousePos.x = e.pageX - $('#<%=groupHistorySwimlanes.ClientID%> .gantt-container').offset().left + $('#<%=groupHistorySwimlanes.ClientID%> .gantt-container').scrollLeft(),
                    currentMousePos.y = e.pageY + $('#<%=groupHistorySwimlanes.ClientID%> .gantt-container').scrollTop()
                });
            });


        </script>
    </ContentTemplate>
</asp:UpdatePanel>
