using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Threading;
using Cysharp.Threading.Tasks;

public static class UniTaskUtil
{
    public static CancellationTokenSource Renew(this CancellationTokenSource source)
    {
        source.Clear();
        return new CancellationTokenSource();
    }

    public static void Clear(this CancellationTokenSource source)
    {
        source.Cancel();
        source.Dispose();
    }

    public static CancellationTokenSource Merge(this CancellationToken to, CancellationToken from)
    {
        return CancellationTokenSource.CreateLinkedTokenSource(to, from);
    }

    public static CancellationTokenSource MergeTokens(CancellationToken to, CancellationToken from)
    {
        return CancellationTokenSource.CreateLinkedTokenSource(to, from);
    }
}
