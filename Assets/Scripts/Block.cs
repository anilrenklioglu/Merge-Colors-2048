using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class Block : MonoBehaviour
{
   public int Value;
   public Vector2 Pos => transform.position;

   public Node Node;

   public Block MergingBlock;

   public bool Merging;
   
   [SerializeField] private  SpriteRenderer _sr;
   [SerializeField] private TextMeshPro _text;
   
   public void Init(GameManager.BlockType type)
   {
      Value = type.Value;
      _sr.color = type.Color;
      _text.text = type.Value.ToString();
   }

   public void SetBlock(Node node)
   {
      if (Node != null) Node.OccupiedBlock = null;
      Node = node;
      Node.OccupiedBlock = this;
   }

   public void MergeBlock(Block blockToMergeWith)
   {
      //Setting the block we are merging with
      MergingBlock = blockToMergeWith;
      
      //Set current node as unpccupied to allow blocks to use it 
      Node.OccupiedBlock = null;
      
      //Set the base block as merging so it does not get used twice
      blockToMergeWith.Merging = true;
   }

   public bool CanMerge(int value) => value == Value && !Merging && MergingBlock == null;

}
