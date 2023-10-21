# AutoCreateShadowCaster2DShape
ShadowCaster2DのShapeをSpriteRendererやCompositeCollider2Dから生成できるようにするEditor拡張

# 導入方法
* unitypackageをダウンロードして使う
* もしくは`ShadowCaster2DAutoShapeEditor.cs`をEditorフォルダに入れる

unitypackageで導入した場合は`Assets/Editor/AutoCreateShadowShape2D/`にスクリプトがあります。

# 使い方
`SpriteRenderer`か`CompositeCollider2D`がすでについているオブジェクトに`ShadowCaster2D`を付けます。

![image](https://github.com/ayaha401/AutoCreateShadowCaster2DShape/assets/75297336/ba6d3eae-3f91-41f3-9f71-52a2b21908ae)<br>
`ShadowCaster2D`がEditor拡張され、`Auto Create Shadow Shape`ボタンと`Delete Shadow Shape`ボタンが作られます

`Auto Create Shadow Shape`でポリゴンに沿ったShapeが形成されます。<br>
`Delete Shadow Shape`でShapeを**完全**に消去します。

# エラー
## SpriteRenderer or CompositeCollider2D is not attached
`Auto Create Shadow Shape`ボタンか`Delete Shadow Shape`ボタンを押したときに出ます。<br>
`SpriteRenderer`か`CompositeCollider2D`が存在しないと出ます。どちらかをアタッチしてください。
