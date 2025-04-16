namespace SmartFin.Services
{
    /// <summary>
    /// Фоновая служба для проверки целей и создания уведомлений в конце месяца
    /// </summary>
    public class GoalNotificationBackgroundService : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;
        public GoalNotificationBackgroundService(
           IServiceProvider serviceProvider)
        {

            _serviceProvider = serviceProvider;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {


            while (!stoppingToken.IsCancellationRequested)
            {
                // Определяем, находимся ли мы в конце месяца
                var isEndOfMonth = IsEndOfMonth();

                if (isEndOfMonth)
                {
                    using (var scope = _serviceProvider.CreateScope())
                    {
                        var goalNotificationService = scope.ServiceProvider.GetRequiredService<NotificationService>();
                        await goalNotificationService.CheckAllGoalsAndNotifyUsersAsync();
                    }

                }

                // Очистка старых уведомлений (раз в неделю)
                var dayOfWeek = DateTime.Today.DayOfWeek;
                // Определяем время до следующей проверки
                // (один раз в день, в идеале в полночь)
                var tomorrow = DateTime.Today.AddDays(1);
                var delay = tomorrow - DateTime.Now;

                await Task.Delay(delay, stoppingToken);
            }
        }

        /// <summary>
        /// Определяет, находимся ли мы в конце месяца (последнее число)
        /// </summary>
        private bool IsEndOfMonth()
        {
            var today = DateTime.Today;
            var lastDayOfMonth = new DateTime(today.Year, today.Month, DateTime.DaysInMonth(today.Year, today.Month));

            return today.Day == lastDayOfMonth.Day;
        }
    }
}