using Grand.Core.Data;
using Grand.Core.Domain.Tasks;
using Grand.Services.Tasks;
using NopBrasil.Plugin.Payments.PagSeguro.Services;

namespace NopBrasil.Plugin.Payments.PagSeguro.Task
{
    public class CheckPaymentTask : ScheduleTask, IScheduleTask
    {
        private readonly IScheduleTaskService _scheduleTaskService;
        private readonly IPagSeguroService _pagSeguroService;
        private readonly IRepository<ScheduleTask> _scheduleTaskRepository;

        public CheckPaymentTask(IScheduleTaskService scheduleTaskService, IPagSeguroService pagSeguroService, IRepository<ScheduleTask> scheduleTaskRepository)
        {
            this._pagSeguroService = pagSeguroService;
            this._scheduleTaskService = scheduleTaskService;
            this._scheduleTaskRepository = scheduleTaskRepository;
        }

        public void Execute() => _pagSeguroService.CheckPayments();

        private ScheduleTask GetScheduleTask()
        {
            ScheduleTask task = _scheduleTaskService.GetTaskByType(GetTaskType());
            if (task == null)
            {
                task = new ScheduleTask();
                task.Type = GetTaskType();
                task.ScheduleTaskName = "PagSeguro Check Payments";
                task.Enabled = true;
                task.StopOnError = false;
                task.TimeInterval = 10;
                task.TimeIntervalChoice = TimeIntervalChoice.EVERY_MINUTES;
            }
            return task;
        }

        protected string GetTaskType() => "NopBrasil.Plugin.Payments.PagSeguro.Task.CheckPaymentTask, NopBrasil.Plugin.Payments.PagSeguro";

        public void InstallTask()
        {
            ScheduleTask scheduleTask = GetScheduleTask();
            if (string.IsNullOrEmpty(scheduleTask.Id))
                _scheduleTaskRepository.Insert(scheduleTask);
            else
                _scheduleTaskService.UpdateTask(scheduleTask);
        }

        public void UninstallTask()
        {
            ScheduleTask scheduleTask = GetScheduleTask();
            if (string.IsNullOrEmpty(scheduleTask.Id))
            {
                _scheduleTaskRepository.Delete(scheduleTask);
            }
        }
    }
}
