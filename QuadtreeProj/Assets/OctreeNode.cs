using System.Collections;
using System.Collections.Generic; // to use generic list&stack
using UnityEngine;
using System.Linq;
public class OctreeNode
{
    public static int maxObjectLimit = 1;

    static OctreeNode _octreeRoot;
    public static OctreeNode octreeRoot   // Singleton method
    {
        get
        {
            if (_octreeRoot == null)
            {
                _octreeRoot = new OctreeNode(null, Vector3.zero, 15f, new List<OctreeItem>());
            }
            return _octreeRoot;
        }
    }


    //Visualize the octree node
    GameObject octantGO; // the gameObject in charge of displaying the boudaries of this particular node
    LineRenderer octantLineRenderer;


    public float halfDimentionLength;
    private Vector3 pos; // the center of the node.

    public OctreeNode parent;
    public List<OctreeItem> containedItems = new List<OctreeItem>(); // what items are contained in this particular octree node..
    OctreeNode[] _childrenNodes = new OctreeNode[8]; // each node has 8 children nodes.
    public OctreeNode[] childrenNodes   //no unauthorized people can change the childrenNodes.
    {
        get { return _childrenNodes; }
    }

    public void EraseChildrenNodes()
    {
        _childrenNodes = new OctreeNode[8];
    }

    [RuntimeInitializeOnLoadMethod] //this method is runned when Unity starts
    static bool Init()
    {
        return octreeRoot == null;
    }

    //constructor
    public OctreeNode(OctreeNode parent, Vector3 thisChild_pos, float thisChild_halfLength, List<OctreeItem> potential_Items)
    {
        this.parent = parent;
        halfDimentionLength = thisChild_halfLength;
        pos = thisChild_pos;

        octantGO = new GameObject();
        octantGO.hideFlags = HideFlags.HideInHierarchy;
        octantLineRenderer = octantGO.AddComponent<LineRenderer>();

        FillCube_VisualizeCoords();

        foreach (OctreeItem item in potential_Items)
        {
            ProcessItem(item);
        }

    }

    public bool ProcessItem(OctreeItem item)
    {
        if (ContainsItemPos(item.transform.position))
        {
            if(ReferenceEquals(childrenNodes[0], null)) // check if the childrenNodes are null
            {
                PushItem(item);

                return true;
            }
            else // if there are childrenNodes, do recursion
            {
                foreach(OctreeNode childNode in childrenNodes)
                {
                    if (childNode.ProcessItem(item))
                    {
                        return true;
                    }
                }
            }
        }

        return false;
    }

    private void PushItem(OctreeItem item) // we know that an item should be accquired and contained by this node.
    {
        if (!containedItems.Contains(item)) // only add it to our list of contained items if its not mentioned yet within the list
        {
            containedItems.Add(item);
            item.my_ownerNodes.Add(this);
        }

        if(containedItems.Count > maxObjectLimit)
        {
            Split();
        }
    }

    private void Split()
    {
        foreach(OctreeItem oi in containedItems) // for every item which was contained in this splitting node:
        {
            oi.my_ownerNodes.Remove(this); // make the item forget about this particular node(since it had to split into smaller children)
        }

        Vector3 positionVector = new Vector3(halfDimentionLength / 2, halfDimentionLength / 2, halfDimentionLength / 2);
        // towards the top right futures child origin

        for(int i = 0; i < 4; i++)
        {
            _childrenNodes[i] = new OctreeNode(this, pos + positionVector, halfDimentionLength / 2, containedItems);
            positionVector = Quaternion.Euler(0f, -90f, 0f) * positionVector;
        }

        positionVector = new Vector3(halfDimentionLength / 2, -halfDimentionLength / 2, halfDimentionLength / 2);
        for (int i = 4; i < 8; i++)
        {
            _childrenNodes[i] = new OctreeNode(this, pos + positionVector, halfDimentionLength / 2, containedItems);
            positionVector = Quaternion.Euler(0f, -90f, 0f) * positionVector;
        }

        containedItems.Clear(); // do not use == null here, since you have to reinstantiate the list again next time.
    }


    public void Attempt_ReduceSubdivisions(OctreeItem escapedItem)
    {
        if( ! ReferenceEquals(this, octreeRoot) && ! Siblings_ChildrenNodesPresent_too_manyItems())
        {
            // Delete node and siblings
            foreach(OctreeNode on in parent.childrenNodes) // iterate through this node and its 7 siblings, them kill them
            {
                on.KillNode(parent.childrenNodes.Where(i => !ReferenceEquals(i, this)).ToArray()); // pass 7 siblings as we will be killing this node.
            }
            parent.EraseChildrenNodes(); //make parent forget about its old, already killed children nodes.

        }
        else // otherwise, of there are children in siblings, or there are too many items for the parent to potentially hold, then: 
        {
            containedItems.Remove(escapedItem); //remove the item from the contained items of this particular node since such item no longer falls into the domain of this node.
            escapedItem.my_ownerNodes.Remove(this);
        }

    }

    private void KillNode(OctreeNode[] obsoleteSiblingNodes)
    {
        foreach(OctreeItem oi in containedItems)
        {
            oi.my_ownerNodes = oi.my_ownerNodes.Except(obsoleteSiblingNodes).ToList(); // from such item's owner node extract a list excluding all the siblings of this obsolete node. Then 
                                                                                       //reassign such list to the owner nodes of that item
            oi.my_ownerNodes.Remove(this); // remove this node as well, after removing its 7 siblings.
            oi.my_ownerNodes.Add(parent);
            parent.containedItems.Add(oi);
        }

        GameObject.Destroy(octantGO);
    }

    private bool Siblings_ChildrenNodesPresent_too_manyItems() // true if the children nodes are present in siblings of this particular obsolete node
                                                               //or if their total number of items is way too much for the parent to accept.
    {
        List<OctreeItem> legacy_items = new List<OctreeItem>(); // items contained in this obsolete node and the siblings

        foreach(OctreeNode sibling in parent.childrenNodes) //iterate through siblings and see if they have any children
        {
            if( ! ReferenceEquals(sibling.childrenNodes[0], null)) // if they do have children then return true ( this obsolete node and its siblings won't get deleted later)
            {
                return true;
            }

            legacy_items.AddRange(sibling.containedItems.Where(i => !legacy_items.Contains(i))); //add all the items from the currently inspected sibling, Add only
            // the items not already contained in our legacy items list.
        }
        
        if (legacy_items.Count > maxObjectLimit + 1 ) // too many items for the parent to hold, Do not get rid of siblings and this particular obsolete node.
        {
            return true;
        }

        return false; // Having lookad at all the siblings and none of them contain child nodes. Their items altogether could be held by the parent. So delete
                      // this particular node and those sibling nodes.
    }


    public bool ContainsItemPos(Vector3 itemPos)
    {
        if (itemPos.x > (pos.x + halfDimentionLength) || itemPos.x < (pos.x - halfDimentionLength))
            return false;
        if (itemPos.y > (pos.y + halfDimentionLength) || itemPos.y < (pos.y - halfDimentionLength))
            return false;
        if (itemPos.z > (pos.z + halfDimentionLength) || itemPos.z < (pos.z - halfDimentionLength))
            return false;

        return true;
    }


    void FillCube_VisualizeCoords()
    {
        Vector3[] cubeCoords = new Vector3[8];
        Vector3 corner = new Vector3(halfDimentionLength, halfDimentionLength, halfDimentionLength);

        for (int i = 0; i < 4; i++)  // populate the first half of cube coords, point towards all 4 top corners
        {
            cubeCoords[i] = pos + corner;
            Debug.Log(" index i is : " + i + " corner.x = " + corner.x);
            Debug.Log(" index i is : " + i + " corner.y = " + corner.y);
            Debug.Log(" index i is : " + i + " corner.z = " + corner.z);
            corner = Quaternion.Euler(0f, 90f, 0f) * corner;
            Debug.Log(" index i is : " + i + " after quaternion , corner.x = " + corner.x);
            Debug.Log(" index i is : " + i + " after quaternion , corner.y = " + corner.y);
            Debug.Log(" index i is : " + i + " after quaternion , corner.z = " + corner.z);
        }

        corner = new Vector3(halfDimentionLength, -halfDimentionLength, halfDimentionLength);
        for (int i = 4; i < 8; i++)  //  point towards all 4 bottom corners
        {
            cubeCoords[i] = pos + corner;
            corner = Quaternion.Euler(0f, 90f, 0f) * corner; // rotate round the vertical axis, pointing to the remaining corners of the cube
        }

        octantLineRenderer.useWorldSpace = true;
        octantLineRenderer.SetVertexCount(16);
        octantLineRenderer.SetWidth(0.03f, 0.03f);
        octantLineRenderer.SetPosition(0, cubeCoords[0]);
        octantLineRenderer.SetPosition(1, cubeCoords[1]);
        octantLineRenderer.SetPosition(2, cubeCoords[2]);
        octantLineRenderer.SetPosition(3, cubeCoords[3]);
        octantLineRenderer.SetPosition(4, cubeCoords[0]);
        octantLineRenderer.SetPosition(5, cubeCoords[4]);
        octantLineRenderer.SetPosition(6, cubeCoords[5]);
        octantLineRenderer.SetPosition(7, cubeCoords[1]);

        octantLineRenderer.SetPosition(8, cubeCoords[5]);
        octantLineRenderer.SetPosition(9, cubeCoords[6]);
        octantLineRenderer.SetPosition(10, cubeCoords[2]);
        octantLineRenderer.SetPosition(11, cubeCoords[6]);
        octantLineRenderer.SetPosition(12, cubeCoords[7]);
        octantLineRenderer.SetPosition(13, cubeCoords[3]);
        octantLineRenderer.SetPosition(14, cubeCoords[7]);
        octantLineRenderer.SetPosition(15, cubeCoords[4]);


    } // end FillCube_VisualizeCoords()



}