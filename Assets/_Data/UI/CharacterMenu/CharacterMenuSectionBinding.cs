using System;
using UnityEngine;

[Serializable]
public class CharacterMenuSectionBinding
{
    [SerializeField] private CharacterMenuSection section;
    [SerializeField] private GameObject viewRoot;
    [SerializeField] private CharacterMenuTabButton tabButton;

    public CharacterMenuSection Section => section;
    public GameObject ViewRoot => viewRoot;
    public CharacterMenuTabButton TabButton => tabButton;
}
