using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Random = UnityEngine.Random;
using DG.Tweening;

public class GameManager : MonoBehaviour
{
   private GameState _state;

   private int _round;
   //-----------------Creating The Grid------------------------//
   [SerializeField] private int _width = 4;
   [SerializeField] private int _height = 4;
   [SerializeField] private Node _nodePrefab;
   [SerializeField] private SpriteRenderer _boardPrefab;

   //-----------------Spawning The Blocks----------------------//
   [SerializeField] private Block _blockPrefab;

   private List<Node> _nodes;
   private List<Block> _blocks;
   
   //--------------------Block Type-----------------------------//

   [SerializeField] private List<BlockType> type;
   
   //--------------------Blocks Moving-----------------------------//
   [SerializeField] private float travelTime = 0.2f;
   
   //--------------------Winning Game-----------------------------//

   [SerializeField] private int _winGameCondition = 2048;
   [SerializeField] private GameObject _winScreen, _loseScreen;
   private BlockType GetBlockTypeByValue(int value) => type.First(t => t.Value == value);
   
   //-----------------------MobileIput---------------------//

   private Vector3 firstPos;
   private Vector3 lastPos;
   private bool moveCompleted;
   private void Start()
   {
      ChangeState(GameState.GenerateLevel);
   }

   private void ChangeState(GameState newState)
   {
      _state = newState;

      switch (newState)
      {
         case GameState.GenerateLevel:
            GenerateGrid();
            break;
         case GameState.SpawningBlocks:
            SpawnBlocks(_round++ == 0 ? 2 : 1);
            break;
         case GameState.WaitingInput:
            break;
         case GameState.Moving:
            break;
         case GameState.Win:
            _winScreen.SetActive( true);
            break;
         case GameState.Lose:
            _loseScreen.SetActive(true);
            break;
         default:
            throw new ArgumentOutOfRangeException(nameof(newState), newState, null);
      } 
   }

   void Update()
   {
      if (_state != GameState.WaitingInput) return;
      
      MobileInput();

#if UNITY_STANDALONE
      
      if (Input.GetKeyDown(KeyCode.LeftArrow)) Shift(Vector2.left);
      if (Input.GetKeyDown(KeyCode.RightArrow)) Shift(Vector2.right);
      if (Input.GetKeyDown(KeyCode.UpArrow)) Shift(Vector2.up);
      if (Input.GetKeyDown(KeyCode.DownArrow)) Shift(Vector2.down);
      
#endif
      
   }

   //-------------Genarating Game Grid-------------------//
   void GenerateGrid()
   {
      _round = 0;
      _nodes = new List<Node>();
      _blocks = new List<Block>();
      
      for (int x = 0; x < _width; x++)
      {
         for (int y = 0; y < _height; y++)
         {
            var node = Instantiate(_nodePrefab, new Vector2(x, y), Quaternion.identity);
            _nodes.Add(node);
         }
      }

      var center = new Vector2((float)_width / 2 - 0.5f, (float)_height / 2 - 0.5f);

      var board = Instantiate(_boardPrefab, center, Quaternion.identity);

      board.size = new Vector2(_width, _height);

      Camera.main.transform.position = new Vector3(center.x, center.y, -10);
      
      ChangeState(GameState.SpawningBlocks);
   }
   
   //----------------Spawning Blocks------------------//
   void SpawnBlocks(int amount)
   {
      var freeNodes = _nodes.Where(n => n.OccupiedBlock == null).OrderBy(b => Random.value).ToList();

      foreach (var node in freeNodes.Take(amount))
      {
         SpawnBlock(node, Random.value > 0.8f ? 4 : 2);
      }
      
      if ( freeNodes.Count() == 1)
      {
        ChangeState(GameState.Lose);
         return;
         
      }
      
      ChangeState(_blocks.Any(b=>b.Value == _winGameCondition) ? GameState.Win : GameState.WaitingInput);
   }

   void SpawnBlock(Node node, int value) {
      var block = Instantiate(_blockPrefab, node.Pos, Quaternion.identity);
      block.Init(GetBlockTypeByValue(value));
      block.SetBlock(node);
      _blocks.Add(block);
   }

   void Shift(Vector2 dir)
   {
      ChangeState(GameState.Moving);
      
      var orderedBlocks = _blocks.OrderBy(b => b.Pos.x).ThenBy(b => b.Pos.y).ToList();

      if (dir == Vector2.right || dir == Vector2.up) orderedBlocks.Reverse();

      foreach (var block in orderedBlocks)
      {
         var next = block.Node;
         
         do
         {
            block.SetBlock(next);

            var possibleNode = GetNodeAtPosition(next.Pos + dir);

            if (possibleNode != null)
            {
               //we know a node is present
               
               //if it's possible to merge then set merge

               if (possibleNode.OccupiedBlock != null && possibleNode.OccupiedBlock.CanMerge(block.Value))
               {
                  block.MergeBlock(possibleNode.OccupiedBlock);
                  
               }
               //Otherwise can we move to this spot ?
               else if (possibleNode.OccupiedBlock == null)
               {
                  next = possibleNode;
               }
               //End do while loop
            }
         } while (next != block.Node);
      }

      var sequance = DOTween.Sequence();

      foreach (var block in orderedBlocks)
      {
         var movePoint = block.MergingBlock !=null ? block.MergingBlock.Node.Pos : block.Node.Pos;

         sequance.Insert(0, block.transform.DOMove(movePoint, travelTime));

      }

      sequance.OnComplete(() =>
      {
         foreach (var block in orderedBlocks.Where(b =>b.MergingBlock != null))
         {
            MergeBlocks(block.MergingBlock,block);
         }
         
         ChangeState(GameState.SpawningBlocks);
         
      });
   }

   void MergeBlocks(Block baseBlock, Block mergingBlock)
   {
      SpawnBlock(baseBlock.Node, baseBlock.Value * 2);
      
      RemoveBlock(baseBlock);
      RemoveBlock(mergingBlock);
   }

   void RemoveBlock(Block block)
   {
      _blocks.Remove(block);
      
      Destroy(block.gameObject);
   }
   Node GetNodeAtPosition(Vector2 pos)
   {
      return _nodes.FirstOrDefault(n => n.Pos == pos);
   }

   void MobileInput ()
   {
      if (Input.GetMouseButtonDown(0))
      {
         firstPos = Input.mousePosition;

         lastPos = firstPos;
      }
      
      else if ( !moveCompleted && Input.GetMouseButton(0))
      {
         lastPos = Input.mousePosition;

         Vector3 deltaPos = lastPos - firstPos;

         firstPos = lastPos;

         if (Mathf.Abs(deltaPos.x) > Mathf.Abs(deltaPos.y))
         {
            if (deltaPos.x > 0)
            {
               Shift(Vector2.right);
               moveCompleted = true;
            }

            else if (deltaPos.x < 0)
            {
               Shift(Vector2.left);
               moveCompleted = true;
            }
         }
         
         else if (Mathf.Abs(deltaPos.y) > Mathf.Abs(deltaPos.x))
         {
            if (deltaPos.y > 0)
            {
               Shift(Vector2.up);
               moveCompleted = true;
            }

            else if (deltaPos.y < 0)
            {
               Shift(Vector2.down);
               moveCompleted = true;
            }
         }
      }
      
      else if ( Input.GetMouseButtonUp(0))
      {
         moveCompleted = false;
      }

      else
      {
         firstPos = Vector3.zero;
         lastPos = Vector3.zero;
      }
   }
   
   
   [Serializable]
  public struct BlockType
  {
     public int Value;
     public Color Color;
  }

  public enum GameState
  {
     GenerateLevel,
     SpawningBlocks,
     WaitingInput,
     Moving,
     Win,
     Lose,
  }
}
