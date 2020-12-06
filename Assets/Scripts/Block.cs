using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public enum BlockState { Valid = 0, Intersecting = 1, OutOfBounds = 1, Placed = 2 }
public class Block {
    public List<Voxel> Voxels;
    public Dictionary<Voxel, bool> VoxelsConnection = new Dictionary<Voxel, bool>();
    public Dictionary<Voxel, List<Voxel>> connectionEnabledDictionary = new Dictionary<Voxel, List<Voxel>>();
    public PatternType Type;
    private Pattern _pattern => PatternManager.GetPatternByType(Type);
    private VoxelGrid _grid;
    private GameObject _goBlock;

    public Vector3Int Anchor;
    public Quaternion Rotation;
    private bool _placed = false;
    /// <summary>
    /// Get the current state of the block. Can be Valid, Intersecting, OutOfBound or Placed
    /// </summary>
    public BlockState State {
        get {
            if (_placed) return BlockState.Placed;
            if (Voxels.Count < _pattern.Indices.Count) return BlockState.OutOfBounds;
            if (Voxels.Count(v => v.Status != VoxelState.Available) > 0) return BlockState.Intersecting;
            return BlockState.Valid;
        }
    }
    /// <summary>
    /// Block constructor. Will create block starting at an anchor with a certain rotation of a given type.
    /// </summary>
    /// <param name="type">The block type</param>
    /// <param name="anchor">The index where the block needs to be instantiated</param>
    /// <param name="rotation">The rotation the blocks needs to be instantiated in</param>
    public Block(PatternType type, Vector3Int anchor, Quaternion rotation, VoxelGrid grid) {
        Type = type;
        Anchor = anchor;
        Rotation = rotation;
        _grid = grid;


        PositionPattern();
    }

    /// <summary>
    /// Add all the relevant voxels to the block according to it's anchor point, pattern and rotation
    /// </summary>
    public void PositionPattern() {
        Voxels = new List<Voxel>();
        foreach (var index in _pattern.Indices) {
            if (Util.TryOrientIndex(index, Anchor, Rotation, _grid, out var newIndex)) {
                Voxel voxel = _grid.Voxels[newIndex.x, newIndex.y, newIndex.z];
                if (index.Equals(new Vector3Int(0,0,0)) || index.Equals(new Vector3Int(7, 0, 0)) || index.Equals(new Vector3Int(0, 0, 7)) || index.Equals(new Vector3Int(7, 0, 7))) {
                    VoxelsConnection.Add(voxel, true);

                    if (_pattern.Type.Equals(PatternType.PatternA)) {
                        List<Voxel> connectionEnabledVoxels = new List<Voxel>();
                        Vector3Int gridSize = _grid.GridSize;
                        Vector3Int bottom = new Vector3Int(-1, 0, 0);
                        Vector3Int bottomWorldIndex;
                        Util.TryOrientIndex(bottom, Anchor, Rotation, _grid, out bottomWorldIndex);
                        if (bottomWorldIndex.x >= 0 && bottomWorldIndex.x < gridSize.x
                            && bottomWorldIndex.y >= 0 && bottomWorldIndex.y < gridSize.y
                            && bottomWorldIndex.z >= 0 && bottomWorldIndex.z < gridSize.z) {
                            Voxel bottomWorldVoxel = _grid.Voxels[bottomWorldIndex.x, bottomWorldIndex.y, bottomWorldIndex.z];
                            connectionEnabledVoxels.Add(bottomWorldVoxel);
                        }

                        Vector3Int top = new Vector3Int(8, 0, 0);
                        Vector3Int topWorldIndex;
                        Util.TryOrientIndex(top, Anchor, Rotation, _grid, out topWorldIndex);
                        if (topWorldIndex.x >= 0 && topWorldIndex.x < gridSize.x 
                            && topWorldIndex.y >= 0 && topWorldIndex.y < gridSize.y
                            && topWorldIndex.z >= 0 && topWorldIndex.z < gridSize.z ) {
                            Voxel topWorldVoxel = _grid.Voxels[topWorldIndex.x, topWorldIndex.y, topWorldIndex.z];
                            connectionEnabledVoxels.Add(topWorldVoxel);
                        }

                        connectionEnabledDictionary.Add(voxel, connectionEnabledVoxels);

                    } else if (_pattern.Type.Equals(PatternType.PatternB)) {

                    }

                }
                else {
                    VoxelsConnection.Add(voxel, false);
                }
                Voxels.Add(voxel);
            }
        }
    }

    /// <summary>
    /// Try to activate all the voxels in the block. This method will always return false if the block is not in a valid state.
    /// </summary>
    /// <returns>Returns true if it managed to activate all the voxels in the grid</returns>
    public bool ActivateVoxels() {
        if (State != BlockState.Valid) {
            Debug.LogWarning("Block can't be placed");
            return false;
        }
        Color randomCol = Util.RandomColor;

        foreach (var voxel in Voxels) {
            voxel.Status = VoxelState.Alive;
            voxel.SetColor(randomCol);

            //Is it a connection voxel
            if (VoxelsConnection[voxel] == true) {
                Debug.Log("I am connection");
                if (_pattern.Type.Equals(PatternType.PatternA)) {
                    List<Voxel> enabledVoxles = connectionEnabledDictionary[voxel];
                    Debug.Log("I am stick connection");
                    Debug.Log(voxel.Index);
                    foreach (Voxel enabledVoxel in enabledVoxles) {
                        enabledVoxel.connectionEnabled = true;
                        Debug.Log("Set one voxel to connection enabled");
                        Debug.Log(enabledVoxel.Index);
                    }
                } else {
                    voxel.connection = true;
                }
            } else {
                voxel.connection = false;
            }

            // voxel.connection = VoxelSConnectionEnabled[voxel];
        }
        CreateGOBlock();
        _placed = true;
        return true;
    }

    public void CreateGOBlock() {
        _goBlock = GameObject.Instantiate(_grid.GOPatternPrefabs[Type], _grid.GetVoxelByIndex(Anchor).Centre, Rotation);
    }

    /// <summary>
    /// Remove the block from the grid
    /// </summary>
    public void DeactivateVoxels() {
        foreach (var voxel in Voxels)
            voxel.Status = VoxelState.Available;
    }

    /// <summary>
    /// Completely remove the block
    /// </summary>
    public void DestroyBlock() {
        DeactivateVoxels();
        if (_goBlock != null) GameObject.Destroy(_goBlock);
    }
}
