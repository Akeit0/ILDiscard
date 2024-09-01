using System;
using System.Linq;
using System.Reflection;
using ILDiscard;
using UnityEngine;


[DiscardMembers(DiscardMembersOptions.DiscardByDefault)]
public enum MyEnum
{
    A,
    B,
    [DontDiscard]
    C
}
[Discard]
class Constants
{
    public const int CONSTVALUE100 = 100;
}
[DiscardMembers]
public class NewScript : MonoBehaviour
{
    [Discard]
    const int  CONSTVALUE = 100;
   
    public MyEnum myEnum;

    [Discard]
    public int Value;
    
    void Start()
    {
        Debug.Log("CONSTVALUE" + CONSTVALUE);
        
        Debug.Log(string.Join(", ",GetType().GetMembers(BindingFlags.NonPublic).Select(x=>x.Name)));
        Application.targetFrameRate=60;
        var a = MyEnum.A;
        var b = MyEnum.B;
        var c = MyEnum.C;
        Debug.Log(string.Join(", ",Enum.GetNames(typeof(MyEnum))));
        Debug.Log((int)a);
        Debug.Log((int)b);
        Debug.Log((int)c);
        
        Debug.Log(Constants.CONSTVALUE100);
        
    }
    
    [Discard]
    void Update()
    {
        Debug.Log("Update");
    }
}