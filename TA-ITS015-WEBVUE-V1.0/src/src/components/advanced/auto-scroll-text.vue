<template>
    <div class="scroll-box" ref="scrollBox">
        <div
            class="scroll-text"
            ref="scrollContent"
            :class="{ 'scroll-animate': state.shouldScroll }"
        >
            {{ text }}
        </div>
    </div>
</template>
  
<script setup>
import { ref, reactive, watch, onMounted, onBeforeUnmount, nextTick } from 'vue';

// Props
const props = defineProps({
    text: {
      type: String,
      required: true
    },
    isShow: {
      type: Boolean,
      default: false
    }
});
  
const state = reactive({
    shouldScroll: false
});
  
// Refs
const scrollBox = ref(null);
const scrollContent = ref(null);
let observer = null;
  
// Kiem tra độ dài chữ có cần scroll hay không
const checkOverflow = () => {
    const box = scrollBox.value;
    const content = scrollContent.value;
  
    if (box && content) {
      const boxWidth = box.clientWidth;
      const contentWidth = content.scrollWidth;
      state.shouldScroll = contentWidth > boxWidth;
    }
};
  
// Quan sát khi hiển thị trong DOM
const observeVisibility = () => {
    if (!scrollBox.value) return;
  
    observer = new IntersectionObserver(async (entries) => {
      const entry = entries[0];
      if (entry.isIntersecting) {
        await nextTick();
        checkOverflow();
      }
    });
  
    observer.observe(scrollBox.value);
};
  
onMounted(() => {
    observeVisibility();
});
  
onBeforeUnmount(() => {
    if (observer && scrollBox.value) {
      observer.unobserve(scrollBox.value);
    }
});
  
// Watch text change
watch(() => props.text, async () => {
    await nextTick();
    checkOverflow();
});

</script>
  
<style scoped>
.scroll-box {
    width: 80%;
    overflow: hidden;
    white-space: nowrap;
    position: relative;
}
.scroll-text {
    display: inline-block;
}
.scroll-animate {
    padding-left: 10%;
    animation: scroll-left 10s linear infinite;
}
@keyframes scroll-left {
    0% { transform: translateX(0); }
    100% { transform: translateX(-80%); }
}
</style>