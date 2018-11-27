using UnityEngine;
using System.Collections;
using System;
using System.Collections.Generic;
using UnityEngine.UI;

/// <summary>
/// 打印机效果
/// </summary>
public class Effect_TypeWriter : MonoBehaviour 
{
    public bool m_OnStarPrint = false;
    
    [Tooltip("结束打印机效果时回调")]
    public List<Action> m_onFinished = new List<Action>();


    [Tooltip("文字显示间隔")]
    private float m_strIntervalTime = 0f;
    
    [Tooltip("结束时运行onfinished的等待时间")]
    private float m_delayOnFinishTime = 0;

    private int m_index = 0;
    private string m_strs;
    private string m_tempStr;
    void Start()
    {
        if (m_OnStarPrint)
        {
            string strs = gameObject.GetComponent<Text>().text;
            m_tempStr = strs;
            PrintStart(m_tempStr);
        }
    }

    /// <summary>
    /// 设置间隔时间
    /// </summary>
    public void SetIntervalTime(float time)
    {
        m_strIntervalTime = time;
    }

    /// <summary>
    /// 开始打印
    /// </summary>
    /// <param name="str">需要现实的文字</param>
    public void PrintStart(string str)
    {
        Common_DelayToInvoke cd;
        if (!m_OnStarPrint)
        {
            Common_DelayToInvoke.Stop();
        }
        m_index = 0;
        m_strs = str;
        gameObject.GetComponent<Text>().text = "";
        cd = new Common_DelayToInvoke(TextAnimator, 0.05f);
    }
    private void PrintFinished()
    {
        if (m_onFinished != null)
        {
            List<Action> mTemp = m_onFinished;
            foreach (Action tempac in mTemp)
            {
                tempac();
            }
        }
    }
    public void TextAnimator()
    {
        Common_DelayToInvoke cd;
        m_index++;
        gameObject.GetComponent<Text>().text = m_strs.Remove(m_index - 1);
        if (m_index == m_strs.Length)
        {
            gameObject.GetComponent<Text>().text = m_strs;
            cd = new Common_DelayToInvoke(PrintFinished, m_delayOnFinishTime);
            //Invoke("PrintFinished", m_delayOnFinish);
        }
        else
        {
            cd = new Common_DelayToInvoke(TextAnimator, m_strIntervalTime);
        }
    }
}
