using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerController : MonoBehaviour
{
    [SerializeField] float duration = 2f;
    [SerializeField] float fallSpeed = 10f;
    [SerializeField] GameObject heldObject;
    
    Vector3 targetPos;
    Rigidbody2D playerRb;
    Animator animator;
    LevelMap level;
    Grid grid;
    GameObject gino;

    public Vector3Int PlayerPos {get; private set;}
    
    public bool isInteracting {get; set;} = false;
    public bool isHoldingSomething {get; set;} = false;
    public bool isFacingLeft {get; private set;} = false;
    public bool isMoving {get; private set;} = false;
    public bool isPushing { get; private set; } = false;
    //public bool isFalling { get; private set; } = false;

    public Interactable selected {get; set;}
    public bool OnSolidGround { get; private set; } = true;


    // Start is called before the first frame update
    void Start()
    {
        //Vector3 myPos = transform.position;
        targetPos = transform.position; //new Vector3(Mathf.Round(myPos.x), myPos.y, myPos.z);
        playerRb = GetComponent<Rigidbody2D>();
        animator = GetComponentInChildren<Animator>();
        selected = null;
        grid = FindObjectOfType<Grid>();
        PlayerPos = grid.WorldToCell(transform.position);
        level = FindObjectOfType<LevelMap>();
        gino = this.gameObject;
    }

    // Update is called once per frame
    void Update()
    {
        if (isMoving || isInteracting) return;  // Let current actions play out
        animator.SetBool("Running", false); 
        
        if (selected)
        {
            TryToMoveTo(grid.WorldToCell(selected.transform.position));
        }
        else if (!OnSolidGround)
        {
            print("Not on solid ground");
            
            Fall();
        }
    }

    public void FlipDirection()
    {   var prevScale = transform.localScale;
        transform.localScale = new Vector3(prevScale.x * -1, prevScale.y, prevScale.z);
        isFacingLeft = !isFacingLeft;
    }

    public void RemoveSelection()
    {
        animator.SetBool("Running", false);
        selected?.Unselect();
        selected = null;
    }

    internal void TryToMoveTo(Vector3Int targetPos)
    {
        if (isMoving || isInteracting) return;  // Let any current actions play out first

        Interactable nextSquare;
        int fallDist = 0, xDist = 0, yDist = 0;
        var levelMap = level.map;

        while (selected && PlayerPos != targetPos && !(isMoving || isInteracting))
        {
            xDist = targetPos.x - PlayerPos.x;
            yDist = targetPos.y - PlayerPos.y;

            // Handle Pushing & pulling
            if (isPushing)
            {
                HandlePush(xDist, yDist);
                break;
            }

            // Handle falling
            Interactable squareBelow = null;
            if (PlayerPos.y > 0) squareBelow = levelMap[PlayerPos.x, (PlayerPos + Vector3Int.down).y];

            if (squareBelow && !squareBelow.canWalkOn)
            {
                if (squareBelow.spaceType == SpaceType.Empty) 
                {
                    fallDist++;
                    //if (fallDist > 1) break;    // You can only fall one square and survive
                    Fall();
                    continue;               
                }
                else
                {
                    break;  // Square below not walkable and not empty
                }
            }

            if (transform.localScale.x * xDist < 0)
            {
                FlipDirection();
            }

            if (xDist != 0)
            {
                int girth = 1;  // How many squares do I take up
                if (isHoldingSomething)
                {
                    girth = 2;  // Right now all held objects are 1 square long
                }
                nextSquare = levelMap[PlayerPos.x + Math.Sign(xDist) * girth, PlayerPos.y];

                if (nextSquare == null) break;  // Hitting wall is a fail 
                if (nextSquare.spaceType == SpaceType.Empty)
                {
                    print("Running?");
                    animator.SetBool("Running", true);
                    MoveOneHoriz(xDist);
                }
                else
                {
                    print("Stop running?");
                    animator.SetBool("Running", false);
                    // Handle interactions
                    if (nextSquare == selected && !isHoldingSomething)
                    {
                        // Reached destination which is non-empty tile
                        if (nextSquare.canBeGrabbed)
                        {
                            GrabObject(nextSquare);
                        }
                        else if (nextSquare.spaceType == SpaceType.FullCrate)
                        {
                            StartPushMode(nextSquare);
                        }
                        else if (nextSquare.spaceType == SpaceType.Digable)
                        {
                            animator.SetTrigger("Digging");
                            nextSquare.ChangeContents("Dug Dirt");
                        }
                    }
                    else if (nextSquare == selected)    // reached selected square while holding something
                    {
                        if (GetHeldObjectType() == "Empty Crate" && nextSquare.spaceType == SpaceType.Dug)
                        {   // Fill crate with dug dirt
                            nextSquare.ChangeContents("Full Crate");
                            level.UpdateMap(nextSquare.GridPos.x, nextSquare.GridPos.y, nextSquare);
                            HoldNothing();
                            break;
                        }
                    }
                    else if (nextSquare.canClimb && level.CanClimb(PlayerPos, nextSquare.GridPos))
                    {
                        ClimbOneSquare();
                        break;
                    }
                    // else nextSquare not selected or invalid (ran into something) so finish
                    RemoveSelection();
                    break;
                }
            }
            else    // selected tile directly above or below player
            {
                //targetPos = PlayerPos;
                RemoveSelection();
                break;
            }
        }
        //
        if (PlayerPos == targetPos || (isHoldingSomething && Math.Abs(xDist) == 1) && yDist == 0)
        {
            RemoveSelection();
        }
    }

    private void HandlePush(int xDist, int yDist)
    {
        if (xDist == 0 && yDist == 0)
        {
            DropHeldObject();
            isPushing = false;
            animator.SetBool("Pushing", false);
            return;
        }
        if (xDist != 0)
        {
            int offset = 0;
            if (transform.localScale.x * Math.Sign(xDist) > 0)  // Pushing rather than pulling
            {
                offset = Math.Sign(xDist);
            }
            var nextSquare = level.map[PlayerPos.x + Math.Sign(xDist) + offset, PlayerPos.y];

            if (nextSquare == null) return;  // Hitting wall is a fail 
            if (nextSquare.spaceType == SpaceType.Empty)
            {
                MoveOneHoriz(xDist);
            }
        }
    }

    private void StartPushMode(Interactable crate)
    {
        isPushing = true;
        animator.SetBool("Running", false);
        animator.SetBool("Pushing", true);
        GrabObject(crate);
    }

    private void ClimbOneSquare()
    {
        StartCoroutine(ClimbCoroutine());
    }

    internal void ChangeDirection()
    {
        bool okayToTurn = true;

        if (isHoldingSomething)
        {
            if (PlayerPos.x == 0 || PlayerPos.x == level.MaxX)
            {
                okayToTurn = false;
            }
            else
            {
                var left = level.map[PlayerPos.x - 1, PlayerPos.y];
                var right = level.map[PlayerPos.x + 1, PlayerPos.y];
                if
                    (!isFacingLeft && (left == null || left.spaceType != SpaceType.Empty) ||
                    (isFacingLeft && (right == null || right.spaceType != SpaceType.Empty)))
                {
                    okayToTurn = false;
                }
            }
        }
        if (okayToTurn)
        {
            FlipDirection();
        }
    }

    internal void DropHeldObject()
    {
        int offset = (isFacingLeft) ? -1 : 1;
        Interactable dest = null;
        try
        {
            dest = level.map[PlayerPos.x + offset, PlayerPos.y];
        }
        catch
        {
            print("Tried to drop object out of bounds");
            return; // if out of bounds for some reason
        }

        if (dest?.spaceType == SpaceType.Empty) // Space directly ahead empty, drop item there
        {
            dest.ChangeContents(GetHeldObjectType());
            level.UpdateMap(PlayerPos.x + offset, PlayerPos.y, dest);
            HoldNothing();
            GravityCheck(dest);
        }
    }

    private void GravityCheck(Interactable thing)
    {
        while (thing.GridPos.y > 0)
        {
            var underneath = level.map[thing.GridPos.x, thing.GridPos.y - 1];
            if (!underneath) return;    // No interactable underneath (typically base tile)

            if (underneath.spaceType == SpaceType.Empty)
            {
                SwapSpaces(thing, underneath);
                thing = underneath;
            }
            else if (!underneath.canWalkOn)
            {
                print("Dropped object landed on non-walkable space and you haven't coded behaviour for this yet!");
                return;
            }
            else    // object dropped on walkable space - done falling
            {
                return;
            }
        }

    }

    private void SwapSpaces(Interactable space1, Interactable space2)
    {
        var temp = space1.name;
        space1.ChangeContents(space2.name);
        space2.ChangeContents(temp);
    }

    private void HoldNothing()
    {
        isHoldingSomething = false;
        if (GetHeldObjectType() == "Full Crate")
        { 
            isPushing = false; 
            animator.SetBool("Pushing", false);
        }
        for (int i = 0; i < heldObject.transform.childCount; i++)
        {
            heldObject.transform.GetChild(i).gameObject.SetActive(false);
        }
    }

    private string GetHeldObjectType()
    {
        for (int i = 0; i < heldObject.transform.childCount; i++)
        {
            GameObject child = heldObject.transform.GetChild(i).gameObject;
            if (child.activeInHierarchy) return child.name;
        }
        return "Nothing";
    }

    private void GrabObject(Interactable nextSquare)
    {
        string objectName = nextSquare.name;
        nextSquare.ChangeContents("Empty");
        level.UpdateMap(nextSquare.GridPos.x, nextSquare.GridPos.y, nextSquare);

        for (int i = 0; i < heldObject.transform.childCount; i++)
        {
            if (heldObject.transform.GetChild(i).name == objectName)
            {
                heldObject.transform.GetChild(i).gameObject.SetActive(true);
            }
            else
            {
                heldObject.transform.GetChild(i).gameObject.SetActive(false);
            }
        }
        isHoldingSomething = true;
        //print("Now holding " + objectName);
        if (objectName == "Full Crate") 
        {
            isPushing = true;
            animator.SetBool("Pushing", true);
        }
    }

    private void MoveOneHoriz(int xDist)
    {
        var target = new Vector3(transform.position.x + Math.Sign(xDist), transform.position.y, transform.position.z);
        StartCoroutine(MoveToTarget(target));
    }

    private IEnumerator MoveToTarget(Vector3 target)
    {
        if (isMoving) yield break;  // Exit if already moving
        animator.SetBool("Running", true);
        isMoving = true;

        float counter = 0;
        Vector3 startPos = transform.position;

        while (counter < duration)
        {
            counter += Time.deltaTime;
            transform.position = Vector3.Lerp(startPos, target, counter / duration);
            yield return null;
        }
        isMoving = false;
        transform.position = target;
        UpdatePlayerPos();
        //animator.SetBool("Running", false);
    }

    private IEnumerator ClimbCoroutine()
    {
        if (isMoving) yield break;  // Exit if already moving
        animator.SetTrigger("Climbing");
        isMoving = true;

        float counter = 0;
        Vector3 startPos = transform.position;
        Vector3 target = transform.position + Vector3.up;

        while (counter < duration / 2)
        {
            counter += Time.deltaTime;
            transform.position = Vector3.Lerp(startPos, target, counter / duration);
            yield return null;
        }

        transform.position = target;
        //animator.SetBool("Running",true);
        counter = 0;
        startPos = transform.position;
        target = transform.position + (transform.localScale.x > 0 ? Vector3.right : Vector3.left);

        while (counter < duration / 2)
        {
            counter += Time.deltaTime;
            transform.position = Vector3.Lerp(startPos, target, counter / duration);
            yield return null;
        }

        isMoving = false;
        transform.position = target;
        UpdatePlayerPos();

    }

    private void UpdatePlayerPos()
    {
        //print("In UpdatePlayerPos, position is " + transform.position);
        PlayerPos = grid.WorldToCell(transform.position);
        Interactable underneath = null;
        if (PlayerPos.y > 0)
        {
            underneath = level.map[PlayerPos.x, PlayerPos.y - 1];
            print("underneath is of type " + (underneath? underneath.spaceType.ToString() : "nothing"));
        }
        else
        {
            OnSolidGround = true;
        }

        if (!underneath || underneath.canWalkOn == true)
        {
            OnSolidGround = true;
        }
        else
        {
            OnSolidGround = false;
        }
    }

    private void Fall()
    {
        Vector3 target = transform.position + Vector3.down;
        StartCoroutine(MoveToTarget(target));
        //isFalling = isMoving;
    }
}
