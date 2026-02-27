using System.Collections;
using ParkingCat.World;
using UnityEngine;
using VContainer;


public sealed class TutorialFxController : MonoBehaviour
{
    [SerializeField] TutorialArrowFxUI arrowFx;
    ICarRuntimeRegistry _runtime;

    [Inject]
    public void Construct(ICarRuntimeRegistry runtime)
    {
        _runtime = runtime;
    }

    public void BindToCar(int carId)
    {
        StopAllCoroutines();
        StartCoroutine(BindWhenReady(carId));
    }

    IEnumerator BindWhenReady(int carId)
    {
        // 차량이 생성될 때까지 대기
        while (true)
        {
            if (_runtime != null && _runtime.TryGet(carId, out var info) && info.PosTf != null)
            {
                arrowFx.Bind(info.PosTf);
                yield break;
            }
            yield return null;
        }
    }

    public void Clear()
    {
        StopAllCoroutines();
        arrowFx.Unbind();
    }
}