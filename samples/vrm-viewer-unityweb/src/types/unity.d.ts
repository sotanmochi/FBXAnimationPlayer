/**
 * Unity WebGL インスタンスの型定義。
 * Unity の loader.js が window に公開する API。
 */
export interface UnityInstance {
  /** Unity GameObject にメッセージを送信する。 */
  SendMessage(objectName: string, methodName: string, value?: string): void
  /** WebGL コンテキストを解放する。 */
  Quit(): Promise<void>
}

export interface UnityConfig {
  dataUrl: string
  frameworkUrl: string
  codeUrl: string
  streamingAssetsUrl?: string
  companyName?: string
  productName?: string
  productVersion?: string
}

declare global {
  function createUnityInstance(
    canvas: HTMLCanvasElement,
    config: UnityConfig,
    onProgress?: (progress: number) => void
  ): Promise<UnityInstance>
}
