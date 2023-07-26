using System;

public class Community : IComparable<Community>
{

    public string ComCode { get; set; }
    public int ComScore { get; set; }

    public int CompareTo(Community compareCom)
    {
        // A null value means that this object is greater.
        if (compareCom == null)
            return 1;

        else
            return this.ComScore.CompareTo(compareCom.ComScore);
    }
}
