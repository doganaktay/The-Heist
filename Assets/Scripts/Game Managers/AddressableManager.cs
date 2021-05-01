using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using Cysharp.Threading.Tasks;
using System.Threading;

public class AddressableManager : MonoBehaviour
{
    #region Data

    // STATIC

    // PUBLIC
    public static AddressableManager Instance;

    // INSTANCE

    // PUBLIC
    public Sprite[] groundTileSprites;

    // PRIVATE
    CancellationToken lifetimeToken;

    #endregion

    void Awake()
    {
        lifetimeToken = gameObject.GetCancellationTokenOnDestroy();

        if (Instance is null)
            Instance = this;
    }

    public async UniTask LoadAddressables()
    {
        var first = await Addressables.LoadAssetAsync<Sprite[]>("Assets/Art/Kenney/ground_tiles_1.png");
        var second = await Addressables.LoadAssetAsync<Sprite[]>("Assets/Art/Kenney/ground_tiles_2.png");
        groundTileSprites = new Sprite[first.Length + second.Length];
        Array.Copy(first, groundTileSprites, first.Length);
        Array.Copy(second, 0, groundTileSprites, first.Length, second.Length);
    }

    public void LoadAddressablesAsync()
    {
        AsyncOperationHandle<Sprite[]> spriteHandle = Addressables.LoadAssetAsync<Sprite[]>("Assets/Art/Kenney/ground_tiles.png");
        spriteHandle.Completed += LoadSpritesWhenReady;
        
    }

    void LoadSpritesWhenReady(AsyncOperationHandle<Sprite[]> handle)
    {
        if (handle.Status == AsyncOperationStatus.Succeeded)
            groundTileSprites = handle.Result;
        else
            Debug.Log("Sprite array addressable not loaded!");
    }

    public Sprite[] GetGroundSprites() => groundTileSprites;

}
