import { ref, computed } from 'vue'
import type { UnityMessageBus } from './useUnityMessageBus'

export type AnimationState = 'playing' | 'paused' | 'stopped'

/**
 * FBX アニメーションの読み込みと再生制御を担当する Composable。
 * useUnityMessageBus を受け取り、アニメーションに特化した状態と操作を提供する。
 */
export function useAnimationController(bus: UnityMessageBus) {
  const isLoaded   = ref(false)
  const isLoading  = ref(false)
  const error      = ref<string | null>(null)
  const clipCount  = ref(0)

  const state       = ref<AnimationState>('stopped')
  const currentTime = ref(0)
  const duration    = ref(0)
  const isLooping   = ref(true)
  const speed       = ref(1.0)

  const normalizedTime = computed(() =>
    duration.value > 0 ? currentTime.value / duration.value : 0
  )
  const isPlaying = computed(() => state.value === 'playing')

  // ── Unity → JS: FBX 読み込み結果 ────────────────────────────────────
  bus.on('fbx/loaded', (payload: any) => {
    isLoading.value = false
    isLoaded.value  = payload?.success === true
    clipCount.value = payload?.clipCount ?? 0
    if (!isLoaded.value) error.value = payload?.message ?? 'FBX load failed'
  })

  bus.on('fbx/error', (payload: any) => {
    isLoading.value = false
    error.value     = payload?.message ?? 'Unknown FBX error'
  })

  // ── Unity → JS: 再生状態・時間更新 ──────────────────────────────────
  bus.on('animation/stateChanged', (payload: any) => {
    state.value = payload?.state ?? 'stopped'
  })

  bus.on('animation/timeUpdated', (payload: any) => {
    currentTime.value = payload?.current ?? 0
    duration.value    = payload?.duration ?? 0
  })

  // ── FBX ファイル読み込み ─────────────────────────────────────────────
  function loadFile(file: File): void {
    isLoading.value = true
    error.value     = null
    const url = URL.createObjectURL(file)
    bus.send('fbx/load', { url })
    setTimeout(() => URL.revokeObjectURL(url), 30_000)
  }

  // ── 再生制御 ─────────────────────────────────────────────────────────
  function play()  { bus.send('animation/play') }
  function pause() { bus.send('animation/pause') }
  function stop()  { bus.send('animation/stop') }

  function seek(normalizedT: number) {
    bus.send('animation/seek', { normalizedTime: Math.max(0, Math.min(1, normalizedT)) })
  }

  function setLooping(enabled: boolean) {
    isLooping.value = enabled
    bus.send('animation/setLooping', { enabled })
  }

  function setSpeed(value: number) {
    speed.value = value
    bus.send('animation/setSpeed', { speed: value })
  }

  function togglePlay() {
    if (state.value === 'playing') pause()
    else play()
  }

  return {
    isLoaded, isLoading, error, clipCount,
    state, isPlaying, currentTime, duration, normalizedTime,
    isLooping, speed,
    loadFile,
    play, pause, stop, togglePlay,
    seek, setLooping, setSpeed,
  }
}
