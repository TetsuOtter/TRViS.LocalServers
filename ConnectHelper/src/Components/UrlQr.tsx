import { Suspense } from "react";
import QRCode from "qrcode";
import { ErrorBoundary } from "react-error-boundary";

export type UrlQrProps = {
	url: string;
};

let dataUrl: string | null = null;

const qrOptions: QRCode.QRCodeToDataURLOptions = {
	errorCorrectionLevel: "H",
	margin: 0,
	width: 512,
};

const LazyUrlQr = ({ url }: UrlQrProps) => {
	if (dataUrl != null) {
		return <img src={dataUrl} />;
	}

	throw QRCode.toDataURL(url, qrOptions).then((_dataUrl) => {
		dataUrl = _dataUrl;
		return <img src={_dataUrl} />;
	});
};

const UrlQr = (props: UrlQrProps) => {
	return (
		<ErrorBoundary fallback={<div>QRコードの生成に失敗しました</div>}>
			<Suspense fallback={<p>QRコードを生成中...</p>}>
				<LazyUrlQr {...props} />
			</Suspense>
		</ErrorBoundary>
	);
};

export default UrlQr;
