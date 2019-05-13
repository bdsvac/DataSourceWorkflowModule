using System.Linq;
using HI.Shared.DataSourceWorkflowModule.Extensions;
using HI.Shared.DataSourceWorkflowModule.Models;
using Sitecore.Diagnostics;
using Sitecore.Pipelines.GetPageEditorNotifications;
using Sitecore.Shell.Framework.CommandBuilders;

namespace HI.Shared.DataSourceWorkflowModule.Pipeline.GetPageEditorNotifications
{
    public class GetDataSourceWorkflowNotification : GetPageEditorNotificationsProcessor
    {
        #region Methods

        public override void Process(GetPageEditorNotificationsArgs arguments)
        {
            Assert.ArgumentNotNull(arguments, "arguments");
            if (arguments.ContextItem == null) return;
            foreach (var dataSourceArguments in arguments.ContextItem.GetAllUniqueDataSourceItems().Select(ds => new GetPageEditorNotificationsArgs(ds)))
            {
                GetNotifications(dataSourceArguments);
                foreach (var n in dataSourceArguments.Notifications)
                {
                    arguments.Notifications.Add(n);
                }
            }
        }

        public void GetNotifications(GetPageEditorNotificationsArgs arguments)
        {
            if (arguments == null) return;
            var wfModel = new ItemWorkflowModel(arguments.ContextItem);
            if (wfModel.ShowNotification)
            {
                SetNotification(arguments, wfModel);
            }
        }

        private void SetNotification(GetPageEditorNotificationsArgs arguments, ItemWorkflowModel wfModel)
        {
            var editorNotification = new PageEditorNotification(wfModel.GetEditorDescription(), PageEditorNotificationType.Warning)
            {
                Icon = wfModel.WorkflowState.Icon
            };
            if (wfModel.HasWriteAccess())
            {
                foreach (var notificationOption in wfModel.Commands.Select(command => new PageEditorNotificationOption(command.DisplayName, new WorkflowCommandBuilder(wfModel.ContextItem, wfModel.Workflow, command).ToString())))
                {
                    editorNotification.Options.Add(notificationOption);
                }
            }
            arguments.Notifications.Add(editorNotification);
        }

        #endregion
    }
}
