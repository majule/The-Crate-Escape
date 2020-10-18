using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Interactable : MonoBehaviour
{
    [SerializeField] GameObject selectedBox;
    [SerializeField] SpaceType _spaceType;
    [SerializeField] Sprite[] sprites;
    [SerializeField] string animationType = "Running";

    PlayerController player;

    public bool canWalkOn = false;
    public bool canClimb = false;
    public bool canBeGrabbed = false;
    public bool canBeDragged = false;

    public Vector3Int GridPos {get; private set;}
    public SpaceType spaceType
    {
        get
        {
            return _spaceType;
        }
        private set 
        {
            _spaceType = value;
        }
    }

    private void OnEnable() 
        {
            GridPos = FindObjectOfType<Grid>().WorldToCell(transform.position);
        }
    
    // Start is called before the first frame update
    void Start()
    {
        selectedBox.SetActive(false);
    }

    internal void Select()
    {
        selectedBox.SetActive(true);
    }

    public void Unselect()
    {
        selectedBox.SetActive(false);
    }

    internal void ChangeContents(string newType)
    {

        if (newType == "Empty")
        {
            spaceType = SpaceType.Empty;
            canClimb = canWalkOn = canBeGrabbed = canBeDragged = false;
        }
        else if (newType == "Empty Crate")
        {
            spaceType = SpaceType.EmptyCrate;
            canClimb = canWalkOn = canBeDragged = false;
            canBeGrabbed = true;
        }
        else if (newType == "Dug Dirt")
        {
            spaceType = SpaceType.Dug;
            canClimb = canWalkOn = canBeGrabbed = canBeDragged = false;
        }
        else if (newType == "Full Crate")
        {
            spaceType = SpaceType.FullCrate;
            canClimb = canWalkOn = canBeDragged = true;
            canBeGrabbed = false;
        }

        gameObject.GetComponent<SpriteRenderer>().sprite = sprites[(int)spaceType];
        gameObject.name = newType;
    }
}
