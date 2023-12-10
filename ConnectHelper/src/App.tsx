import { useEffect, useState } from "react";
import UrlQr from "./Components/UrlQr";

import licenses_txt from "./assets/licenses.txt";

import "./App.css";

const App = () => {
	const [licenseText, setLicenseText] = useState<string>("");
	const params = new URLSearchParams(window.location.search);
	const host = params.get("host") ?? "";
	const port = parseInt(params.get("port") ?? "");
	const isParamError = !host || !port || !(0 < port && port < 65536);
	const url = `http://${host}:${port}/timetable.json`;
	const trvisUrl = `trvis://app/open/json?path=${url}`;

	useEffect(() => {
		fetch(licenses_txt)
			.then((res) => res.text())
			.then((text) => setLicenseText(text));
	}, []);

	const readQrGuide = !isParamError && (
		<>
			<p>
				TRViSがインストールされた端末でこのQRコードを読み取ってください。
				<br />
				(スマートフォンとこのマシンが同じネットワークに属している必要があります)
			</p>

			<p>または、下のURLをTRViSにて入力してください。</p>
			<a href={url}>{url}</a>
		</>
	);

	return (
		<>
			<h1>TRViS連携用QRコード</h1>
			<div
				style={{
					margin: "0 auto",
					width: "75vmin",
				}}>
				{isParamError ? (
					<span style={{ color: "red", fontWeight: "bold" }}>
						パラメータが不正です
						<br />
						(host: {host}, port: {port})
					</span>
				) : (
					<UrlQr url={trvisUrl} />
				)}
			</div>

			{readQrGuide}

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
