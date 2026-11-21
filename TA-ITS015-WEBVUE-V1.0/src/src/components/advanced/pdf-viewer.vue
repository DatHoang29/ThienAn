<template>
  <div>
    <canvas ref="pdfCanvas"></canvas>
  </div>
</template>

<script setup>

import { ref, onMounted } from 'vue'
import * as pdfjsLib from 'pdfjs-dist'

pdfjsLib.GlobalWorkerOptions.workerSrc = '/pdf.worker.mjs' // ✅ ES module worker/ 👈 This is key with Vite!


const pdfCanvas = ref(null)

onMounted(async () => {
  const loadingTask = pdfjsLib.getDocument('https://ontheline.trincoll.edu/images/bookdown/sample-local-pdf.pdf')
  const pdf = await loadingTask.promise
  const page = await pdf.getPage(1)

  const viewport = page.getViewport({ scale: 1.5 })
  const canvas = pdfCanvas.value
  const context = canvas.getContext('2d')

  canvas.height = viewport.height
  canvas.width = viewport.width

  const renderContext = {
    canvasContext: context,
    viewport: viewport,
  }

  await page.render(renderContext).promise
})
</script>
