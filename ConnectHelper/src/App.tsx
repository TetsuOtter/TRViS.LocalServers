import { useCallback, useEffect, useState } from "react";
import UrlQr from "./Components/UrlQr";

import licenses_txt from "./assets/licenses.txt";

import "./App.css";
import CurrentData from "./Components/CurrentData";

const App = () => {
	const [licenseText, setLicenseText] = useState<string>("");
	const [hasCurrentDataLoadError, setHasCurrentDataLoadError] =
		useState<boolean>(false);
	const [forceShowQr, setForceShowQr] = useState<boolean>(false);

	const params = new URLSearchParams(window.location.search);
	const host = params.get("host") ?? "";
	const port = parseInt(params.get("port") ?? "");
	const isParamError = !host || !port || !(0 < port && port < 65536);
	const url = `http://${host}:${port}/timetable.json`;
	const trvisUrl = `trvis://app/open/json?path=${url}`;

	const enableForceShowQr = useCallback(() => {
		setForceShowQr(true);
	}, []);

	useEffect(() => {
		fetch(licenses_txt)
			.then((res) => res.text())
			.then((text) => setLicenseText(text));
	}, []);

	return (
		<>
			<h1>TRViS連携用QRコード</h1>
			<div
				style={{
					margin: "0 auto",
					padding: "0.5em",
					width: "75vmin",
					border: "solid 1px black",
				}}>
				{isParamError ? (
					<span style={{ color: "red", fontWeight: "bold" }}>
						パラメータが不正です
						<br />
						(host: {host}, port: {port})
					</span>
				) : !forceShowQr && hasCurrentDataLoadError ? (
					<>
						<p>
							シナリオが読み込まれていない、または時刻表サーバと接続できないため、
							<br />
							QRコードを非表示にしています。
						</p>
						<p>
							シナリオ読み込み前は時刻表データが存在しないため、
							<br />
							QRコードを読み込んでも
							<b> TRViSでデータの読み込みに失敗します</b>。
						</p>
						<p>
							それでもQRコードを表示したい場合は、下のボタンを押下してください。
							<br />
							<button
								style={{
									margin: "0.5em",
									padding: "0.5em",
								}}
								onClick={enableForceShowQr}>
								強制的にQRコードを表示
							</button>
						</p>
					</>
				) : (
					<UrlQr
						url={trvisUrl}
						style={{
							width: "100%",
						}}
					/>
				)}
			</div>

			{!isParamError && (
				<>
					<p>
						ゲーム側でシナリオを読み込んだ後、TRViSがインストールされた端末でこのQRコードを読み取ってください。
						<br />
						(スマートフォンとこのマシンが同じネットワークに属している必要があります)
					</p>

					<p>
						または、下のURLをTRViSにて入力してください。
						<br />
						<a href={url}>{url}</a>
					</p>
				</>
			)}

			{!isParamError && (
				<CurrentData
					host={host}
					port={port}
					setHasError={setHasCurrentDataLoadError}
				/>
			)}

			<p>
				本アプリケーションは、以下のパッケージを使用しています。
				<br />
				各パッケージのライセンスの詳細は、各パッケージのリポジトリをご確認頂くか、プラグインに同梱の
				<code>LICENSE-NODE.md</code>
				をご確認ください。
			</p>
			<details>
				<summary>
					依存パッケージとライセンス・リポジトリ一覧
					(ビルド時のみ使用のパッケージを含む)
				</summary>
				<pre>
					<code>{licenseText}</code>
				</pre>
			</details>
		</>
	);
};

export default App;
