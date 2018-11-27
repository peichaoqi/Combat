/************************************************************
  Copyright (C), 2007-2017,BJ Rainier Tech. Co., Ltd.
  FileName: TestMain.cs
  Author: 裴超琦      Version :1.0          Date: 2017年02月24日
  Description:      
************************************************************/
using UnityEngine;
using System.Collections;

public class TestMain : MonoBehaviour
{
    private string[] tempstr =
    {
        "1235456685687568568",
        "qwjertyuiopsdfghjkl",
        "我真的还想载货我百年",
    };

    private int m_index=0;
    public Effect_TypeWriter left;
    public Effect_TypeWriter right;
	private void Start () 
	{
	
	}
	
	
	private void Update ()
    {
        if (Input.GetKeyDown(KeyCode.Space))
        {
            left.PrintStart(tempstr[m_index]);
            m_index++;
        }
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            Common_DelayToInvoke.Clear();
        }
	}
}
