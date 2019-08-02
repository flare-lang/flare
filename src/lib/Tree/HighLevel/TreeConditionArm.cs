namespace Flare.Tree.HighLevel
{
    sealed class TreeConditionArm
    {
        public TreeReference Condition { get; }

        public TreeReference Body { get; }

        public TreeConditionArm(TreeReference condition, TreeReference body)
        {
            Condition = condition;
            Body = body;
        }
    }
}
