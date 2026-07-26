using System;
using UnityEngine;

[Serializable]
public class CharacterMenuSectionBinding
{
    [SerializeField] private CharacterMenuSection section;
    [SerializeField] private GameObject viewRoot;
    [SerializeField] private CharacterMenuTabButton tabButton;

    public CharacterMenuSectionBinding(
        CharacterMenuSection section,
        GameObject viewRoot,
        CharacterMenuTabButton tabButton)
    {
        this.section = section;
        this.viewRoot = viewRoot;
        this.tabButton = tabButton;
    }

    public CharacterMenuSection Section => section;
    public GameObject ViewRoot => viewRoot;
    public CharacterMenuTabButton TabButton => tabButton;

    public void SetViewRoot(GameObject value)
    {
        viewRoot = value;
    }

    public void SetTabButton(CharacterMenuTabButton value)
    {
        tabButton = value;
    }
}
