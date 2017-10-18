using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class OctreeItem : MonoBehaviour {
	public List<OctreeNode> my_ownerNodes = new List<OctreeNode>();

    private Vector3 prevPos;

	// Use this for initialization
	void Start () {
        prevPos = transform.position;
	}

	// Update is called once per frame
	void FixedUpdate () {
        if(transform.position != prevPos)
        {
            RefreshOwners(); // call it as soon as this item has moved.
            prevPos = transform.position;
        }
	}

    public void RefreshOwners()
    {
        OctreeNode.octreeRoot.ProcessItem(this);

        List<OctreeNode> surviveNodes = new List<OctreeNode>(); //store nodes that keep containing the item.
        List<OctreeNode> obsoleteNodes = new List<OctreeNode>(); //during the function store any nodes that are no longer containing the items in this list.
        
        foreach(OctreeNode on in my_ownerNodes)
        {
            if( !on.ContainsItemPos(transform.position))
            {
                obsoleteNodes.Add(on);
            }
            else
            {
                surviveNodes.Add(on);
            }
        }

        my_ownerNodes = surviveNodes;

        foreach(OctreeNode on in obsoleteNodes)
        {
            on.Attempt_ReduceSubdivisions(this);
        }
    }




}
