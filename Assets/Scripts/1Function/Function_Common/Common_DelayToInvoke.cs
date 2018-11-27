using System;
using UnityEngine;
using System.Collections;
/// <summary>
/// 延时调用类
/// </summary>
public class Common_DelayToInvoke : MonoBehaviour
{
    private static GameObject m_tempObj;//执行方法的实体
    public Common_DelayToInvoke(){}

    /// <summary>
    /// 构造方法
    /// </summary>
    /// <param name="action">延时调用的事件</param>
    /// <param name="delaySeconds">等待的事件</param>
    public Common_DelayToInvoke(Action action, float delaySeconds)
    {
        if (m_tempObj)
        {
            m_tempObj.GetComponent<Common_DelayToInvoke>().StartCoroutine(DelayToInvokeDo(action, delaySeconds));
        }
        else
        {
            m_tempObj = new GameObject("IEnumeratorObj");
            m_tempObj.AddComponent<Common_DelayToInvoke>().StartCoroutine(DelayToInvokeDo(action, delaySeconds));
        }
    }

    /// <summary>
    /// 取消所有协同，删除物体
    /// </summary>
    public static void Clear()
    {
        Stop();
        if (m_tempObj)
        {
            DestroyImmediate(m_tempObj);
        }
    }

    /// <summary>
    /// 停止延时调用
    /// </summary>
    public static void Stop()
    {
        if (m_tempObj)
        {
            m_tempObj.GetComponent<Common_DelayToInvoke>().StopAllCoroutines();
        }
    }


    private IEnumerator DelayToInvokeDo(Action action, float delaySeconds)
    {
        yield return new WaitForSeconds(delaySeconds);
        action();
    }
}
