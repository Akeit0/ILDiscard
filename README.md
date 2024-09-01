# ILDiscard
Discard member from IL using ILPostProcessor in Unity.
lThis may be useful in IL2CPP to prevent reading const and enum from the metadata.


# How To Use
```cs
using ILDiscard;
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
        Debug.Log(Constants.CONSTVALUE100);
    }
}
```
# Installation
### Package Manager
1. Open the Package Manager by going to Window > Package Manager.
2. Click on the "+" button and select "Add package from git URL".
3. Enter the following URL:

```
https://github.com/Akeit0/ILDiscard.git?path=/Assets/ILDiscard
```
### manifest.json
Open `Packages/manifest.json` and add the following in the `dependencies` block:
```json
"com.akeit0.ildiscard": "https://github.com/Akeit0/ILDiscard.git?path=/Assets/ILDiscard"
```
# LICENSE

[MIT](https://github.com/Akeit0/ILDiscard/blob/master/LICENSE)
