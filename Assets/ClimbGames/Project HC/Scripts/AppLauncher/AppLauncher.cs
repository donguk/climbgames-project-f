namespace ClimbGames.Project_HC
{
    public partial class AppLauncher
    {
        public enum StepType
        {
            VersionCheck,
            UserPermission,
            Patch,
            PrepareSDK,
        }

        public abstract class LaunchStep : TaskSequencer.StepBase
        {
            public abstract StepType Type { get; }
        }

        private TaskSequencer controller = new TaskSequencer();

        public bool Result { get; private set; }
        public bool IsRunning => controller.IsRunning;
        public float ProgressValue => controller.TotalProgress;
        public string ProgressText
        {
            get
            {
                float value = controller.TotalProgress * 100f;
                return $"{value: 0.0}% ({controller.CurrentIndex}/{controller.TotalCount})";
            }
        }

        public AppLauncher()
        {
            controller.AddStep(new VersionCheck())
                        .AddStep(new UserPermission())
                        .AddStep(new Patch())
                        .AddStep(new PrepareSDK());
        }

        public void Start()
        {
            Result = false;
            controller.Start((result) => Result = result);
        }

        public void Clear() => controller.Dispose();
    }
}