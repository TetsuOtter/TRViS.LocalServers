import { useCallback, useEffect, useState } from "react";

type CurrentDataProps = {
	host: string;
	port: number;
};

interface CurrentScenarioInfo {
	routeName: string;
	scenarioName: string;
	carName: string;
}

const requestInit: RequestInit = {};

const REQUEST_INTERVAL = 30 * 1000;

const CurrentData = ({ host, port }: CurrentDataProps) => {
	const [currentScenarioInfo, setCurrentScenarioInfo] =
		useState<CurrentScenarioInfo | null>(null);
	const [errorMessage, setErrorMessage] = useState<string>("");

	const loadCurrentScenarioInfo = useCallback(() => {
		try {
			fetch(`http://${host}:${port}/scenario-info.json`, {
				...requestInit,
				signal: AbortSignal.timeout(REQUEST_INTERVAL),
			})
				.then(async (res) => {
					if (res.status === 204) {
						throw new Error("シナリオが読み込まれていません");
					}
					if (!res.ok) {
						throw new Error(
							`TRViS用データの読み込みに失敗しました (${res.status})`
						);
					}

					const data = await res.json();
					setCurrentScenarioInfo({
						routeName: data.routeName,
						scenarioName: data.scenarioName,
						carName: data.carName,
					});
				})
				.catch((e) => {
					setCurrentScenarioInfo(null);
					console.error(e);
					if (
						(e instanceof DOMException && e.name === "AbortError") ||
						e instanceof TypeError
					) {
						setErrorMessage(
							"時刻表サーバと接続できません (ネットワークエラー/タイムアウト)"
						);
					} else if (e instanceof Error) {
						setErrorMessage(`データの読み込みに失敗しました (${e.message})`);
					} else {
						setErrorMessage("データの読み込みに失敗しました (不明なエラー)");
					}
				});
		} catch (e) {
			console.error(e);
		}
	}, [host, port]);

	useEffect(() => {
		if (window.fetch == null) {
			setErrorMessage(
				"このブラウザはfetch APIをサポートしていないため、この機能は使用できません"
			);
			return;
		}

		loadCurrentScenarioInfo();
		const intervalId = setInterval(loadCurrentScenarioInfo, REQUEST_INTERVAL);
		return () => clearInterval(intervalId);
	}, [loadCurrentScenarioInfo]);

	return (
		<div
			style={{
				border: "solid 1px black",
			}}>
			<h3>現在読み込まれているデータ</h3>
			{currentScenarioInfo == null ? (
				<p>{errorMessage}</p>
			) : (
				<>
					<p>路線名: {currentScenarioInfo?.routeName}</p>
					<p>シナリオ名: {currentScenarioInfo?.scenarioName}</p>
					<p>車両名: {currentScenarioInfo?.carName}</p>
				</>
			)}
		</div>
	);
};

export default CurrentData;
