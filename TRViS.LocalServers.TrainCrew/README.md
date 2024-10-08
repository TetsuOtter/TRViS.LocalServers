# TRAIN CREW 向け TRViS 連携用ローカルサーバ

このソフトウェアは、TRViS と TRAIN CREW を連携させるためのローカルサーバです。
TRAIN CREW の情報を TRViS に送信することができます。

具体的な使用方法については、Wiki をご覧ください。
-> https://github.com/TetsuOtter/TRViS.LocalServers/wiki/How-To-Install-(TRAIN-CREW)

## 注意事項

- このソフトウェアは、あなたのパソコンにとても簡易な HTTP サーバを立てます。信頼できないネットワークに接続している場合は、このソフトウェアを使用しないでください。
- TRAIN CREW から取得できる情報には限界があるため、TRViS の全ての機能を網羅できているわけではありません。

## ライセンス

このソフトウェアは、MIT ライセンスのもとで公開されています。
また、このソフトウェアにはサードパーティのライブラリが複数含まれています。

- C#で使用しているサードパーティライブラリは、TRAIN CREW 関係を除きすべて Microsoft によって配布されているものです
- QR コード表示ページに使用しているライブラリのライセンスについては、同梱の LICENSE-NODE.md をご覧ください

TRAIN CREW連携用ライブラリである「TrainCrewInput.dll」は、溝月レイル さまが開発・配布しているものです。再配布が許可されているため、同梱して配布しています。
