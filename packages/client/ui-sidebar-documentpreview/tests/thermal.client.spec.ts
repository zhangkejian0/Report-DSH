/** Radiometric detection, analysis-tab addressing, and the preview's analyze action wiring. */
import { Context } from '@deepseek-ai/cordis'
import { SidebarRightTabRegistry } from '@deepseek-ai/dsh-client-ui-sidebar-right/src/client/tab-registry.ts'
import { describe, expect, it, vi } from 'vitest'
import { apply, thermalAnalysisDefinition } from '../src/client/thermal/index.ts'
import {
  analysisAddressFor, isRadiometricJpeg, parseAnalysisAddress, THERMAL_ANALYSIS_ADDRESS,
} from '../src/client/thermal/thermal-address.ts'

/** A JPEG that ends at EOI, followed by `size` filler bytes. */
function jpegWithTrailing(size: number): Uint8Array {
  const body = new Uint8Array(6 + size)
  body.set([0xFF, 0xD8, 0xFF, 0xE0, 0xFF, 0xD9], 0)
  return body
}

describe('radiometric detection', () => {
  it('requires a JPEG SOI and a final EOI pair before a large trailing payload', () => {
    expect(isRadiometricJpeg(jpegWithTrailing(16_384))).toBe(true)
    expect(isRadiometricJpeg(jpegWithTrailing(16_383))).toBe(false)
    expect(isRadiometricJpeg(jpegWithTrailing(0))).toBe(false)
    expect(isRadiometricJpeg(new Uint8Array([0x89, 0x50, 0x4E, 0x47]))).toBe(false)
    expect(isRadiometricJpeg(new Uint8Array(0))).toBe(false)
  })
})

describe('analysis tab addressing', () => {
  const file = 'dsh-resource://file/session/s1/C:/ir/IR_20210306_0007.jpg'
  it('round-trips one jpg file address', () => {
    expect(parseAnalysisAddress(analysisAddressFor(file))).toBe(file)
  })
  it('claims only session-scoped jpg addresses', () => {
    const definition = thermalAnalysisDefinition()
    expect(definition.canOpen?.(analysisAddressFor(file))).toBe(true)
    const png = 'dsh-resource://file/session/s1/photo.png'
    expect(definition.canOpen?.(analysisAddressFor(png))).toBe(false)
    expect(definition.canOpen?.(`${THERMAL_ANALYSIS_ADDRESS}${encodeURIComponent(png)}`)).toBe(false)
    expect(definition.canOpen?.('dsh-resource://file/session/s1/photo.jpg')).toBe(false)
    expect(definition.title(analysisAddressFor(file))).toBe('IR_20210306_0007.jpg')
  })
})

describe('registration', () => {
  it('registers the image action and the analysis tab body under their keys', async () => {
    const ctx = new Context()
    const registered: Array<{ name: string; key?: string | undefined }> = []
    ctx.provide('slots', {
      inject: vi.fn((_name: string, register: () => () => void) => register()),
      register: vi.fn((options: { name: string; key?: string }) => {
        registered.push({ name: options.name, key: options.key })
        return () => {}
      }),
    } as never)
    ctx.provide('sidebarRightTabs', new SidebarRightTabRegistry(ctx) as never)
    ctx.provide('locale', { bind: () => (key: string) => key, register: () => () => {} } as never)
    apply(ctx)
    expect(registered.map(entry => entry.key)).toEqual([
      '@deepseek-ai/dsh-client-ui-sidebar-documentpreview/image',
      '@deepseek-ai/dsh-client-ui-sidebar-documentpreview/thermal-analysis',
    ])
    await ctx.fiber.dispose()
  })
})
