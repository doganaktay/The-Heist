[System.Serializable]
public struct MinMaxData
{
    public float min, max;

    public MinMaxData(float min, float max)
    {
        this.min = min;
        this.max = max;
    }

    public float GetRandomInRange() => GameManager.Random.NextSingle(min, max);

}