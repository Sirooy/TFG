using System.Collections.Generic;
using Core;
using AI;

namespace Cmps
{
    public class AICmp
    {
        public DecisionTreeNode DecisionTree;
        public List<Entity> CurrentTargets;

        public AICmp()
        {
            CurrentTargets = new List<Entity>();
        }
    }
}
