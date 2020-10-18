using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class InputManager : MonoBehaviour
{
    PlayerController player;
    Grid grid;

    float lastClickedTime = 0;
    Interactable lastClickedObject = null;
    bool doubleClicked = false;
    const float DOUBLE_CLICK_TIME = 0.3f;

    private void Start()
    {
        grid = FindObjectOfType<Grid>();
        player = FindObjectOfType<PlayerController>();
    }

    private void Update()
    {
        if (Input.GetMouseButtonDown(0))
        {
            HandleClick();
        }
    }

    private void HandleClick()
    {
        RaycastHit2D hit = Physics2D.GetRayIntersection(Camera.main.ScreenPointToRay(Input.mousePosition));

        if (!hit) return;

        var selectedSquare = hit.collider.gameObject.GetComponent<Interactable>();

        if (selectedSquare)
        { 
            CheckForInteraction(selectedSquare);
        }
    }

    private void CheckForInteraction(Interactable selectedSquare)
    {
        if (player.isInteracting /*|| player.isMoving*/) return;


        var selectedPos = grid.WorldToCell(selectedSquare.transform.position);

        //print("Selected " + gameObject.name + " at " + GridPos);
        var distX = Math.Abs(selectedSquare.GridPos.x - player.PlayerPos.x);
        var distY = selectedSquare.GridPos.y - player.PlayerPos.y;
        //print(distX + " : " + distY + " from player");

        if (distX == 0 && distY == 0)   // Clicking on player swaps his facing direction
        {
            if (player.isHoldingSomething && !player.isPushing)
            {
                var clickTime = Time.time;
                if ((clickTime - lastClickedTime > DOUBLE_CLICK_TIME) || (selectedSquare != lastClickedObject))
                {
                    doubleClicked = false;
                    StartCoroutine(FlipOrDrop());
                }            
                else
                {
                    doubleClicked = true;
                }     
                lastClickedTime = Time.time;
                lastClickedObject = selectedSquare;
            }
            else if (player.isPushing)
            {
                player.DropHeldObject();
            }
            else
            {
                player.FlipDirection();
            }
        }
        else
        {
            player.selected?.Unselect();
            player.selected = selectedSquare;
            selectedSquare.Select();
        }
    }

    IEnumerator FlipOrDrop()
    {
        yield return new WaitForSeconds(DOUBLE_CLICK_TIME);

        if (!doubleClicked)
        {
            player.ChangeDirection();
        }
        else
        {
            player.DropHeldObject();
        }
    }
}
