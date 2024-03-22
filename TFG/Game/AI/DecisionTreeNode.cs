using System;
using System.Collections.Generic;
using Cmps;
using Core;
using Engine.Ecs;

namespace AI
{
    public abstract class DecisionTreeNode
    {
        public abstract DecisionTreeNode Run(EntityManager<Entity> entityManager,
            Entity enemy, AICmp ai);
        
    }

    public abstract class DecisionNode : DecisionTreeNode
    {
        protected TargetSelector targetSelector;

        public DecisionNode(TargetSelector targetSelector)
        {
            this.targetSelector = targetSelector;
        }
    }

    public class BinaryDecisionNode : DecisionNode
    {
        protected Condition condition;
        protected DecisionTreeNode trueNode;
        protected DecisionTreeNode falseNode;

        public BinaryDecisionNode(TargetSelector targetSelector, Condition condition,
            DecisionNode trueNode, DecisionNode falseNode) : base(targetSelector)
        {
            this.condition = condition;
            this.trueNode  = trueNode;
            this.falseNode = falseNode;
        }

        public override DecisionTreeNode Run(EntityManager<Entity> entityManager,
            Entity enemy, AICmp ai)
        {
            targetSelector.Select(entityManager, enemy, ai);

            if (condition.IsTrue(entityManager, enemy, ai))
                return trueNode.Run(entityManager, enemy, ai);
            else
                return falseNode.Run(entityManager, enemy, ai);
        }
    }

    public class TargetChangerDecisionNode : DecisionNode
    {
        protected DecisionTreeNode node;

        public TargetChangerDecisionNode(TargetSelector targetSelector,
            DecisionTreeNode node) : base(targetSelector) 
        {
            this.targetSelector = targetSelector;
            this.node           = node;
        }

        public override DecisionTreeNode Run(EntityManager<Entity> entityManager,
            Entity enemy, AICmp ai)
        {
            targetSelector.Select(entityManager, enemy, ai);

            return node.Run(entityManager, enemy, ai);
        }
    }
}
