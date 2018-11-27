/************************************************************
  Copyright (C), 2007-2017,BJ Rainier Tech. Co., Ltd.
  FileName: QuickEdit.cs
  Author: 万剑飞       Version: 4.8        Date: 2017年12月25日
  Description: 快捷编辑工具，放在Editor文件夹下
  1. Alt+1：获取物体的Transform属性，与下一个连用（注：获取的是Local属性）
  2. Alt+2：将获取的Transform属性赋值给当前选中的物体，可按照在Hierarchy中的顺序进行多个赋值
  3. Alt+3：改变选中物体的中心点（若选中物体带有Renderer或其子类对象的组件，则创建一个新物体作为选中物体的父物体，该物体的位置在选中物体的中心点上）
  4. Alt+4：为选中物体添加BoxCollider，根据本身及所有子物体创建
  5. Alt+5：将选中的物体及其所有子物体合并mesh
  6. Alt+Q：选择选中物体的父物体
  7. Alt+`：显示/隐藏选中的一个或多个物体
  8. Alt+Shift+1/2/3：复制一个或多个选中物体的位置/旋转/缩放属性到剪贴板
************************************************************/

using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

public class QuickEdit
{
    private static Transform[] cpTransforms;//复制的transform属性
    private static bool isCopy = false;//是否已经进行了复制

    #region UGUI物体锚点设置为当前自定义位置
    [MenuItem("uGUI/Anchors to Corners %1")]
    static void AnchorsToCorners()
    {
        foreach (Transform transform in Selection.transforms)
        {
            RectTransform t = transform as RectTransform;
            RectTransform pt = Selection.activeTransform.parent as RectTransform;

            if (t == null || pt == null) return;

            Vector2 newAnchorsMin = new Vector2(t.anchorMin.x + t.offsetMin.x / pt.rect.width,
                                                t.anchorMin.y + t.offsetMin.y / pt.rect.height);
            Vector2 newAnchorsMax = new Vector2(t.anchorMax.x + t.offsetMax.x / pt.rect.width,
                                                t.anchorMax.y + t.offsetMax.y / pt.rect.height);

            t.anchorMin = newAnchorsMin;
            t.anchorMax = newAnchorsMax;
            t.offsetMin = t.offsetMax = new Vector2(0, 0);
        }
    }
    
    #endregion
    #region (CopyTransform)复制选中物体的Transform 快捷键：Alt+1

    [MenuItem("QuickEdit/CopyTransform &1", false, 1)]
    private static void CopyTransform()
    {
        cpTransforms = Selection.transforms;
        isCopy = true;
    }

    #endregion

    #region (PasteTranform)将复制的属性粘贴给选中的物体 快捷键：Alt+2

    [MenuItem("QuickEdit/PasteTranform &2", false, 2)]
    private static void PasteTranform()
    {
        if (!isCopy)
        {
            Debug.Log("请先进行复制操作!");
            return;
        }

        Transform[] targets = Selection.transforms;

        if (targets.Length != cpTransforms.Length)
        {
            Debug.Log("复制与粘贴的物体数量不一致，请确保两次选择的数量相等！");
            return;
        }

        //按层级顺序排序
        OrderTransformsBySiblingIndex(ref targets);
        OrderTransformsBySiblingIndex(ref cpTransforms);

        //注册物体的取消操作
        RegisterUndo("Paste Change", targets);

        for (int i = 0; i < cpTransforms.Length; i++)
        {
            targets[i].localPosition = cpTransforms[i].localPosition;
            targets[i].localEulerAngles = cpTransforms[i].localEulerAngles;
            targets[i].localScale = cpTransforms[i].localScale;
        }
    }

    #endregion

    #region (ChangeCenter)改变中心 快捷键：Alt+3

    [MenuItem("QuickEdit/ChangeCenter &3", false, 51)]
    private static void ChangeCenter()
    {
        Transform parent = Selection.activeTransform;
        DoChangeCenter(parent);
    }

    private static void DoChangeCenter(Transform parent)
    {
        bool isCreateNewObject = false;
        GameObject newObject = null;

        //如果当前选择的物体不是空物体，则需要创建新物体作为父物体
        if (parent.GetComponent<Renderer>() != null)
        {
            newObject = new GameObject(parent.name + "_Center");
            Undo.RegisterCreatedObjectUndo(newObject, "ChangeCenter");
            isCreateNewObject = true;
        }

        Vector3 position = parent.position;
        Quaternion rotation = parent.rotation;
        Vector3 scale = parent.localScale;
        parent.position = Vector3.zero;
        parent.rotation = Quaternion.Euler(Vector3.zero);
        parent.localScale = Vector3.one;

        Vector3 center = Vector3.zero;
        Renderer[] renders = parent.GetComponentsInChildren<Renderer>();
        foreach (Renderer child in renders)
        {
            center += child.bounds.center;
        }
        center /= parent.GetComponentsInChildren<Transform>().Length;
        Bounds bounds = new Bounds(center, Vector3.zero);
        foreach (Renderer child in renders)
        {
            bounds.Encapsulate(child.bounds);
        }

        parent.position = position;
        parent.rotation = rotation;
        parent.localScale = scale;

        //创建了新物体，则将新物体放在中心位置，再将选中的物体作为新物体的子物体
        if (isCreateNewObject)
        {
            newObject.transform.parent = parent.parent;
            newObject.transform.position = bounds.center + position;

            Undo.SetTransformParent(parent, newObject.transform, "ChangeCenter");
            parent.parent = newObject.transform;

            Selection.activeGameObject = newObject;
        }
        //没有创建新物体，则直接改变选中的物体的位置，并将子物体进行偏移
        else
        {
            foreach (Transform t in parent)
            {
                RegisterUndo("ChangeCenter", t);
                t.position = t.position - bounds.center;
            }
            RegisterUndo("ChangeCenter", parent);
            parent.transform.position = bounds.center + parent.position;
        }
    }

    #endregion

    #region (CreateBoxCollider)创建盒子碰撞器 快捷键：Alt+4

    [MenuItem("QuickEdit/CreateBoxCollider &4", false, 52)]
    private static void CreateBoxCollider()
    {
        Transform selection = Selection.activeTransform;

        Collider[] colliders = selection.GetComponentsInChildren<Collider>();
        if (colliders.Length > 0)
        {
            for (int i = 0; i < colliders.Length; i++)
                Undo.DestroyObjectImmediate(colliders[i]);
        }

        BoxCollider bc = Undo.AddComponent<BoxCollider>(selection.gameObject);

        Vector3 pos = selection.position;
        Quaternion rot = selection.rotation;
        Vector3 scale = selection.localScale;
        selection.position = Vector3.zero;
        selection.rotation = Quaternion.Euler(Vector3.zero);
        selection.localScale = Vector3.one;

        Renderer[] renders = selection.GetComponentsInChildren<Renderer>();
        Vector3 center = Vector3.zero;
        foreach (Renderer child in renders)
            center += child.bounds.center;
        center /= renders.Length;
        Bounds bounds = new Bounds(center, Vector3.zero);
        foreach (Renderer child in renders)
            bounds.Encapsulate(child.bounds);

        bc.center = bounds.center - selection.position;
        bc.size = bounds.size;

        selection.position = pos;
        selection.rotation = rot;
        selection.localScale = scale;
    }

    #endregion

    //#region (CombineMesh)合并mesh 快捷键：Alt+5

    //[MenuItem("QuickEdit/CombineMesh &5", false, 53)]
    //private static void CombineMesh()
    //{
    //    Transform selection = Selection.activeTransform;

    //    MeshFilter[] meshFilters = selection.GetComponentsInChildren<MeshFilter>();
    //    int count = meshFilters.Length;

    //    List<CombineInstance> combine = new List<CombineInstance>();
    //    List<Material> mats = new List<Material>();
    //    Matrix4x4 matrix = selection.worldToLocalMatrix;
    //    for (int i = 0; i < count; i++)
    //    {
    //        MeshFilter mf = meshFilters[i];
    //        MeshRenderer mr = mf.GetComponent<MeshRenderer>();
    //        if (mr == null)
    //            continue;

    //        RegisterUndo("CombineMesh", mr);

    //        for (int j = 0; j < mf.sharedMesh.subMeshCount; j++)
    //        {
    //            CombineInstance ci = new CombineInstance();
    //            ci.mesh = mf.sharedMesh;
    //            ci.subMeshIndex = j;
    //            ci.transform = matrix * mf.transform.localToWorldMatrix;
    //            combine.Add(ci);
    //        }
    //        mr.enabled = false;

    //        foreach (var mat in mr.sharedMaterials)
    //            mats.Add(mat);
    //    }

    //    MeshFilter thisMeshFilter = selection.GetComponent<MeshFilter>();

    //    if (thisMeshFilter == null)
    //        thisMeshFilter = Undo.AddComponent<MeshFilter>(selection.gameObject);

    //    RegisterUndo("CombineMesh", thisMeshFilter);

    //    Mesh mesh = new Mesh();
    //    mesh.name = "Combined";
    //    thisMeshFilter.mesh = mesh;
    //    mesh.CombineMeshes(combine.ToArray(), false);

    //    MeshRenderer thisMeshRenderer = selection.GetComponent<MeshRenderer>();
    //    if (thisMeshRenderer == null)
    //        thisMeshRenderer = Undo.AddComponent<MeshRenderer>(selection.gameObject);

    //    RegisterUndo("CombineMesh", thisMeshRenderer);

    //    thisMeshRenderer.sharedMaterials = mats.ToArray();
    //    thisMeshRenderer.enabled = true;
    //}

    //#endregion

    #region (SelectParent)选择当前选择的物体的父物体 快捷键：Alt+Q

    [MenuItem("QuickEdit/SelectParent &q", false, 101)]
    private static void SelectParent()
    {
        Transform target = Selection.activeTransform;

        if (target == null)
        {
            Debug.Log("请选择一个物体！");
            return;
        }

        if (target.parent == null)
        {
            Debug.Log("该物体没有父物体！");
            return;
        }

        RegisterUndo("Select Parent", Selection.activeTransform);
        Selection.activeTransform = target.parent;
    }

    #endregion

    #region (ActiveSelections)显示/隐藏选中的物体 快捷键：Alt+`

    [MenuItem("QuickEdit/ActiveSelections &`", false, 102)]
    private static void ActiveSelections()
    {
        Transform[] targets = Selection.transforms;

        if (targets.Length <= 0)
        {
            Debug.Log("请至少选择一个物体！");
            return;
        }

        if (IsAllObjectsActiveSame(targets))//全部相同则改变为另一种状态
            ChangeObjectsState(targets, !targets[0].gameObject.activeSelf);
        else//有不相同的则全部显示
            ChangeObjectsState(targets, true);
    }

    /// <summary>
    /// 改变物体的状态
    /// </summary>
    /// <param name="state"></param>
    private static void ChangeObjectsState(Transform[] targets, bool state)
    {
        for (int i = 0; i < targets.Length; i++)
        {
            //注册物体的取消操作
            RegisterUndo("Active Change", targets[i].gameObject);

            //改变物体active属性
            targets[i].gameObject.SetActive(state);
        }
    }

    /// <summary>
    /// 判断选中的所有物体
    /// </summary>
    /// <returns>true表示状态全部相同，false表示状态有不同</returns>
    private static bool IsAllObjectsActiveSame(Transform[] targets)
    {
        for (int i = 1; i < targets.Length; i++)
        {
            if (targets[i].gameObject.activeSelf != targets[0].gameObject.activeSelf)
                return false;
        }

        return true;
    }

    #endregion

    #region 复制Transform的属性到剪贴板(位置(CopyPositionToClipboard) : Alt+Shift+1； 旋转(CopyRotationToClipboard) : Alt+Shift+2； 缩放(CopyScaleToClipboard) : Alt+Shift+3)

    private enum InfoType { Position, Rotation, Scale }

    /// <summary>
    /// 复制position
    /// </summary>
    [MenuItem("QuickEdit/CopyPositionToClipboard &#1", false, 151)]
    private static void CopyPositionToClipboard()
    {
        CopyTransformInfo(InfoType.Position);
    }

    /// <summary>
    /// 复制rotation
    /// </summary>
    [MenuItem("QuickEdit/CopyRotationToClipboard &#2", false, 152)]
    private static void CopyRotationToClipboard()
    {
        CopyTransformInfo(InfoType.Rotation);
    }

    /// <summary>
    /// 复制scale
    /// </summary>
    [MenuItem("QuickEdit/CopyScaleToClipboard &#3", false, 153)]
    private static void CopyScaleToClipboard()
    {
        CopyTransformInfo(InfoType.Scale);
    }

    /// <summary>
    /// 根据类型获取对应Transform信息，并复制到粘贴板
    /// </summary>
    /// <param name="type"></param>
    private static void CopyTransformInfo(InfoType type)
    {
        if (Selection.transforms.Length == 0)
        {
            Debug.Log("请选择物体");
            return;
        }

        Transform[] tfs = Selection.transforms;
        OrderTransformsBySiblingIndex(ref tfs);

        Vector3[] v3 = new Vector3[tfs.Length];
        for (int i = 0; i < v3.Length; i++)
        {
            switch (type)
            {
                case InfoType.Position:
                    v3[i] = tfs[i].localPosition;
                    break;
                case InfoType.Rotation:
                    v3[i] = tfs[i].localEulerAngles;
                    break;
                case InfoType.Scale:
                    v3[i] = tfs[i].localScale;
                    break;
            }
        }

        CopyVec3ToClipboard(v3, type);
    }

    /// <summary>
    /// 将选择的向量复制到粘贴板
    /// </summary>
    /// <param name="v3"></param>
    private static void CopyVec3ToClipboard(Vector3[] v3, InfoType type)
    {
        string varName = type.ToString().ToLower();

        //复制的内容
        string content = v3.Length == 1 ? "Vector3 " + varName + " = " : "Vector3[] " + varName + "s = new Vector3[] { ";

        for (int i = 0; i < v3.Length; i++)
        {
            float x = v3[i].x;
            float y = v3[i].y;
            float z = v3[i].z;

            content += "new Vector3(" + x + "f, " + y + "f, " + z + "f)";

            if (i != v3.Length - 1)
                content += ", ";
        }

        content += v3.Length > 1 ? " };" : ";";

        //复制到剪贴板
        TextEditor te = new TextEditor();
        te.text = content;
        te.OnFocus();
        te.Copy();
    }

    #endregion

    #region 按照层级位置进行排序

    /// <summary>
    /// 根据在层级中的位置对transform数组进行排序
    /// </summary>
    private static void OrderTransformsBySiblingIndex(ref Transform[] tfs)
    {
        int minIndex = tfs.Length;
        int minSibling = int.MaxValue;

        for (int i = 0; i < tfs.Length; i++)
        {
            minSibling = int.MaxValue;
            for (int j = i; j < tfs.Length; j++)
            {
                int siblingIndex = tfs[j].GetSiblingIndex();
                if (siblingIndex < minSibling)
                {
                    minIndex = j;
                    minSibling = siblingIndex;
                }
            }
            Swap(ref tfs[i], ref tfs[minIndex]);
        }
    }

    /// <summary>
    /// 交换两个变量
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="a"></param>
    /// <param name="b"></param>
    private static void Swap<T>(ref T a, ref T b)
    {
        T tmp = a;
        a = b;
        b = tmp;
    }

    #endregion

    #region 注册Undo事件，可以取消操作

    /// <summary>
    /// 注册物体的取消操作
    /// </summary>
    /// <param name="name">取消操作的名字</param>
    /// <param name="objects">目标物体</param>
    static private void RegisterUndo(string name, params Object[] objects)
    {
        if (objects != null && objects.Length > 0)
        {
            UnityEditor.Undo.RecordObjects(objects, name);
        }
    }

    #endregion
}
