using UnityEngine;

[System.Serializable]
public struct Tile
{
    public GameObject tile;
    public Rule[] rules;

    public Tile(GameObject _tile, int _ruleCount)
    {
        tile = _tile;
        rules = new Rule[_ruleCount];
    }
}
