using UnityEditor;
using UnityEditor.Animations;
using System.Collections.Generic;
using UnityEngine;

public static class ArmoredOrcAnimatorFlowUtility
{
    private const string AnimatorPath = "Assets/_Data/Characters/Enemy/Animation/ArmoredOrc/ArmoredOrc.controller";

    [MenuItem("Loot Knights/Animation/Configure ArmoredOrc Flow")]
    private static void Configure()
    {
        AnimatorController controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(AnimatorPath);
        if (controller == null)
            return;

        EnsureTrigger(controller, "Block");
        EnsureTrigger(controller, "ArmoredOrc_Thrust");
        EnsureTrigger(controller, "ArmoredOrc_Sweep");

        AnimatorStateMachine machine = controller.layers[0].stateMachine;
        AnimatorState move = FindState(machine, "Move");
        AnimatorState block = FindState(machine, "ArmoredOrc_Block");
        AnimatorState thrust = FindState(machine, "ArmoredOrc_Thrust");
        AnimatorState sweep = FindState(machine, "ArmoredOrc_Sweep");
        if (move == null || block == null || thrust == null || sweep == null)
            return;

        RemoveAnyStateTransitions(machine, block, thrust, sweep);
        EnsureTransition(move, block, "Block");
        EnsureTransition(move, thrust, "ArmoredOrc_Thrust");
        EnsureTransition(thrust, sweep, "ArmoredOrc_Sweep");

        EditorUtility.SetDirty(controller);
        AssetDatabase.SaveAssets();
    }

    private static void EnsureTrigger(AnimatorController controller, string name)
    {
        foreach (AnimatorControllerParameter parameter in controller.parameters)
        {
            if (parameter.type == AnimatorControllerParameterType.Trigger && parameter.name == name)
                return;
        }

        controller.AddParameter(name, AnimatorControllerParameterType.Trigger);
    }

    private static AnimatorState FindState(AnimatorStateMachine machine, string name)
    {
        foreach (ChildAnimatorState child in machine.states)
        {
            if (child.state.name == name)
                return child.state;
        }

        return null;
    }

    private static void RemoveAnyStateTransitions(AnimatorStateMachine machine, params AnimatorState[] states)
    {
        List<AnimatorStateTransition> transitionsToRemove = new List<AnimatorStateTransition>();

        foreach (AnimatorStateTransition transition in machine.anyStateTransitions)
        {
            foreach (AnimatorState state in states)
            {
                if (transition.destinationState != state)
                    continue;

                transitionsToRemove.Add(transition);
                break;
            }
        }

        foreach (AnimatorStateTransition transition in transitionsToRemove)
            machine.RemoveAnyStateTransition(transition);
    }

    private static void EnsureTransition(AnimatorState from, AnimatorState to, string trigger)
    {
        foreach (AnimatorStateTransition transition in from.transitions)
        {
            if (transition.destinationState != to)
                continue;

            foreach (AnimatorCondition condition in transition.conditions)
            {
                if (condition.parameter == trigger)
                    return;
            }
        }

        AnimatorStateTransition created = from.AddTransition(to);
        created.hasExitTime = false;
        created.duration = 0f;
        created.AddCondition(AnimatorConditionMode.If, 0f, trigger);
    }
}
