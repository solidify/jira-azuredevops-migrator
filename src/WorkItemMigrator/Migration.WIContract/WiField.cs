namespace Migration.WIContract
{
    public class WiField
    {
        public string ReferenceName { get; set; }
        public object Value { get; set; }

        public override string ToString()
        {
            return $"[{ReferenceName}]={Value}";
        }
    }

    public static class WiFieldReference
    {
        public static string Title => "System.Title";
        public static string ActivatedDate => "Microsoft.VSTS.Common.ActivatedDate";
        public static string ClosedDate => "Microsoft.VSTS.Common.ClosedDate";
        public static string ClosedBy => "Microsoft.VSTS.Common.ClosedBy";
        public static string AreaPath => "System.AreaPath";
        public static string IterationPath => "System.IterationPath";
        public static string State => "System.State";
        public static string CreatedBy => "System.CreatedBy";
        public static string ChangedBy => "System.ChangedBy";
        public static string AssignedTo => "System.AssignedTo";
        public static string ChangedDate => "System.ChangedDate";
        public static string CreatedDate => "System.CreatedDate";
        public static string ReproSteps => "Microsoft.VSTS.TCM.ReproSteps";
        public static string Description => "System.Description";
        public static string ActivatedBy => "Microsoft.VSTS.Common.ActivatedBy";
        public static string Tags => "System.Tags";
        public static string TeamProject => "System.TeamProject";
        public static string WorkItemType => "System.WorkItemType";
        public static string History => "System.History";

    }
}