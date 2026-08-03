using System.Collections.Generic;
using Photon.Pun;
using UnityEngine;

public sealed class HeroSkillLoadoutPhotonSync : MonoBehaviourPunCallbacks
{
    [SerializeField] private SkillTreeDefinition skillTree;
    [SerializeField] private SkillTreeDefinition[] linkedSkillTrees;
    [SerializeField] private int slotCount = 4;

    private HeroCtrl hero;
    private PlayerSkillTreeManager skillTreeManager;

    private bool IsLocalOwner => photonView == null ||
                                 !PhotonNetwork.InRoom ||
                                 photonView.IsMine;

    public SkillTreeDefinition SkillTree => skillTree;

    private void Awake()
    {
        LoadComponents();
    }

    public override void OnEnable()
    {
        base.OnEnable();
        LoadComponents();

        if (!IsLocalOwner)
            return;

        skillTreeManager = PlayerSkillTreeManager.Service;
        skillTreeManager.OnChanged += PublishLocalLoadout;
        PublishLocalLoadout();
    }

    public override void OnDisable()
    {
        if (skillTreeManager != null)
            skillTreeManager.OnChanged -= PublishLocalLoadout;

        skillTreeManager = null;
        base.OnDisable();
    }

    public void SetSkillTree(SkillTreeDefinition tree)
    {
        skillTree = tree;

        if (isActiveAndEnabled && IsLocalOwner)
            PublishLocalLoadout();
    }

    public void SetSkillTrees(SkillTreeDefinition primaryTree, params SkillTreeDefinition[] additionalTrees)
    {
        skillTree = primaryTree;
        linkedSkillTrees = additionalTrees;

        if (isActiveAndEnabled && IsLocalOwner)
            PublishLocalLoadout();
    }

    public SkillTreeDefinition FindSkillTreeContainingNode(string nodeId)
    {
        return PlayerSkillTreeManager.Service.FindTreeContainingNode(GetSkillTrees(), nodeId);
    }

    public void PublishLocalLoadout()
    {
        if (hero == null)
            LoadComponents();

        IReadOnlyList<SkillTreeDefinition> trees = GetSkillTrees();
        if (trees.Count == 0 || hero == null)
            return;

        PlayerSkillTreeManager manager = skillTreeManager != null
            ? skillTreeManager
            : PlayerSkillTreeManager.Service;

        string[] nodeIds = manager.GetEquippedActiveSkillNodeIds(trees, slotCount);
        string specialNodeId = manager.GetEquippedSpecialSkillNodeId(trees);
        manager.ApplyEquippedSkillsToHero(hero, trees, slotCount);

        if (!PhotonNetwork.InRoom || photonView == null)
            return;

        photonView.RPC(
            nameof(ApplyNetworkLoadout),
            RpcTarget.OthersBuffered,
            GetNodeId(nodeIds, 0),
            GetNodeId(nodeIds, 1),
            GetNodeId(nodeIds, 2),
            GetNodeId(nodeIds, 3),
            specialNodeId ?? string.Empty);
    }

    [PunRPC]
    private void ApplyNetworkLoadout(string slot0, string slot1, string slot2, string slot3, string specialSkill)
    {
        if (IsLocalOwner)
            return;

        if (hero == null)
            LoadComponents();

        string[] nodeIds = { slot0, slot1, slot2, slot3 };
        PlayerSkillTreeManager.Service.ApplyEquippedSkillNodeIdsToHero(hero, GetSkillTrees(), nodeIds, slotCount, specialSkill);
    }

    private void LoadComponents()
    {
        if (hero == null)
            hero = GetComponent<HeroCtrl>();
    }

    private static string GetNodeId(IReadOnlyList<string> nodeIds, int index)
    {
        return nodeIds != null && index >= 0 && index < nodeIds.Count
            ? nodeIds[index] ?? string.Empty
            : string.Empty;
    }

    public IReadOnlyList<SkillTreeDefinition> GetSkillTrees()
    {
        List<SkillTreeDefinition> trees = new();
        if (skillTree != null)
            trees.Add(skillTree);

        if (linkedSkillTrees != null)
        {
            foreach (SkillTreeDefinition linkedTree in linkedSkillTrees)
            {
                if (linkedTree != null && !trees.Contains(linkedTree))
                    trees.Add(linkedTree);
            }
        }

        return trees;
    }
}
