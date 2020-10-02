using UnityEngine;

[System.Serializable]
public class Tile
{
    public GameObject tile;
    public Rule[] rules;
    public Rule selectedRule;

    public Tile(GameObject _tile, int _ruleCount, Rule _selectedRule)
    {
        tile = _tile;
        rules = new Rule[_ruleCount];
        selectedRule = _selectedRule;
    }
}
