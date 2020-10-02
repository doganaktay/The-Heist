
[System.Serializable]
public struct Rule
{
    public int cardinal;
    public int diagonal;
    public float rotation;

    public Rule(int  _cardinal, int _diagonal, float _rotation)
    {
        cardinal = _cardinal;
        diagonal = _diagonal;
        rotation = _rotation;
    }
}
