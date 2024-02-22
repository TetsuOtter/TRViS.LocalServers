import { memo, useEffect, useState } from "react";

import licenses_txt from "../assets/licenses.txt";

export default memo(function LicenseInfo() {
	const [licenseText, setLicenseText] = useState<string>("");

	useEffect(() => {
		fetch(licenses_txt)
			.then((res) => res.text())
			.then((text) => setLicenseText(text));
	}, []);

	return (
		<>
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
});
