# 📘 Bảng Đối Chiếu Tư Duy Frontend: Vue 3 (Composition API) vs React & Kinh Nghiệm Thực Chiến

> 📌 **Tài liệu tham khảo nội bộ**: Dành cho lập trình viên làm việc song song hoặc chuyển đổi giữa Vue 3 và React, tổng hợp các bẫy thường gặp về Reactivity, Lifecycle, Styling/CSS Scope, và kinh nghiệm xử lý UI/Responsive thực tế tại Thiên Ân.

---

## 🧭 1. Tư Duy Cốt Lõi (Mental Model Comparison)

| Đặc tính | Vue 3 (Composition API + `<script setup>`) | React (Function Components + Hooks) |
|---|---|---|
| **Cơ chế Reactivity** | **Proxy-based (Fine-grained)**<br>Biến `ref`/`reactive` được bọc bởi ES6 Proxy. Khi giá trị thay đổi, chỉ đúng component hoặc DOM node phụ thuộc được cập nhật. | **Immutability-based (Re-render)**<br>Khi state thay đổi (`setState`), toàn bộ thân hàm component được **thực thi lại từ đầu đến cuối**. |
| **Thực thi Component** | Thân `<script setup>` **chỉ chạy đúng 1 lần duy nhất** khi component mount. Template re-render tự động dựa trên dependency graph. | Hàm component chạy lại **mỗi khi có bất kỳ state/prop nào thay đổi**. Cần chú ý stale closure và render loop. |
| **Trạng thái (State)** | `const count = ref(0)` (truy cập bằng `count.value`)<br>`const state = reactive({ name: '' })` | `const [count, setCount] = useState(0)`<br>Bắt buộc dùng hàm setter, không được mutate trực tiếp. |
| **Giá trị phái sinh (Computed)** | `const double = computed(() => count.value * 2)`<br>Tự động track dependency, tự cache, không cần khai báo deps. | `const double = useMemo(() => count * 2, [count])`<br>Bắt buộc tự khai báo mảng dependencies `[count]`. |
| **Side Effects / Watchers** | `watch(count, (newVal) => { ... })`<br>`watchEffect(() => { ... })` | `useEffect(() => { ... }, [count])`<br>Chạy sau render. Cần cẩn thận cleanup function và deps array. |
| **Truy cập DOM (Ref)** | `const inputRef = ref<HTMLInputElement>()`<br>Template: `<input ref="inputRef" />` | `const inputRef = useRef<HTMLInputElement>(null)`<br>JSX: `<input ref={inputRef} />` |
| **Chia sẻ Context** | `provide('key', value)` / `inject('key')` | `const MyContext = createContext()`<br>`<MyContext.Provider value={...}>` / `useContext(MyContext)` |
| **Template vs JSX** | **Template-first** (SFC `.vue`): phân tách rõ ràng Template, Script, Style. Tối ưu hoá compile-time tốt. | **JSX / TSX**: HTML viết trong JavaScript. Linh hoạt tối đa nhưng đòi hỏi kỷ luật cao về re-render. |

---

## 🔄 2. Bảng Đối Chiếu Cú Pháp Thường Dùng (Code Cheat Sheet)

### 2.1. Khai báo State & Cập nhật
```typescript
// 🟢 VUE 3
import { ref, reactive } from 'vue';

const count = ref(0);
count.value++; // Mutate tự nhiên qua .value

const form = reactive({ name: 'ThienAn', active: true });
form.active = false; // Mutate trực tiếp thuộc tính object
```

```typescript
// 🔵 REACT
import { useState } from 'react';

const [count, setCount] = useState(0);
setCount(prev => prev + 1); // Bắt buộc setter

const [form, setForm] = useState({ name: 'ThienAn', active: true });
setForm(prev => ({ ...prev, active: false })); // Bắt buộc sao chép immutability
```

---

### 2.2. Vòng Đời Component (Lifecycle Hooks)

| Ngữ cảnh | Vue 3 Composition API | React Hooks |
|---|---|---|
| **Mounting (Khởi tạo)** | `onMounted(() => { ... })` | `useEffect(() => { ... }, [])` |
| **Updating (Cập nhật)** | `onUpdated(() => { ... })` hoặc `watch` | `useEffect(() => { ... }, [dependencies])` |
| **Unmounting (Hủy)** | `onBeforeUnmount(() => { ... })`<br>`onUnmounted(() => { ... })` | `useEffect(() => { return () => { ... } }, [])` (Cleanup) |
| **DOM sẵn sàng trước paint** | `onMounted` + `nextTick(() => { ... })` | `useLayoutEffect(() => { ... }, [])` |

---

### 2.3. Data Binding & Form Input

```html
<!-- 🟢 VUE 3: Two-way binding trực quan qua v-model -->
<el-input v-model="state.keyword" placeholder="Nhập từ khóa" clearable />
```

```tsx
// 🔵 REACT: Controlled Component (1-way data flow)
<Input 
  value={keyword} 
  onChange={(e) => setKeyword(e.target.value)} 
  placeholder="Nhập từ khóa" 
  allowClear 
/>
```

---

### 2.4. Truyền Dữ Liệu Cha - Con (Props & Events)

```vue
<!-- 🟢 VUE 3: Props + Emits -->
<!-- Con: Child.vue -->
<script setup lang="ts">
const props = defineProps<{ title: string; count?: number }>();
const emit = defineEmits<{ (e: 'search', val: string): void }>();
function submit() { emit('search', 'data'); }
</script>

<!-- Cha: Parent.vue -->
<Child :title="'Danh sách'" @search="handleSearch" />
```

```tsx
// 🔵 REACT: Props callback
// Con: Child.tsx
interface Props {
  title: string;
  count?: number;
  onSearch: (val: string) => void;
}
export const Child: React.FC<Props> = ({ title, count, onSearch }) => {
  return <button onClick={() => onSearch('data')}>{title}</button>;
};

// Cha: Parent.tsx
<Child title="Danh sách" onSearch={handleSearch} />
```

---

### 2.5. Slots vs Children

```vue
<!-- 🟢 VUE 3: Named Slots & Scoped Slots -->
<template #header>
  <h3>Tiêu đề card</h3>
</template>
<template #row="{ row }">
  <span>{{ row.name }}</span>
</template>
```

```tsx
// 🔵 REACT: props.children / Render Props
<Card 
  header={<h3>Tiêu đề card</h3>}
  renderRow={(row) => <span>{row.name}</span>}
>
  {/* children */}
</Card>
```

---

## 🎨 3. Styling, Scoped CSS & Deep Selector

### 3.1. Cô lập CSS (CSS Scoping)
- **Vue 3**: Dùng `<style scoped>`. Vue tự động gắn attribute `data-v-xxxx` lên các thẻ HTML trong template và selector CSS.
  - Khi cần can thiệp CSS của component con bên thứ ba (như Element Plus, VxeTable): BẮT BUỘC dùng `:deep(.child-class)` (thay cho `::v-deep` hay `/deep/` cũ).
  - Không dùng comment `//` trong file SCSS (vì compiler Vite sẽ báo lỗi build); luôn dùng `/* */`.
- **React**:
  - Dùng **CSS Modules** (`import styles from './index.module.scss'` $\rightarrow$ `<div className={styles.container}>`).
  - Hoặc **Tailwind CSS** / **Styled-Components** (`const Container = styled.div...`).

### 3.2. Cẩn trọng với `:deep()` trong Sass / Scoped CSS
```scss
/* 🟢 VUE 3 chuẩn xác */
.my-container {
  /* Gắn :deep đúng vị trí component con bên thứ ba */
  :deep(.el-card) {
    .el-card__body {
      padding: 10px;
    }
  }
}
```

---

## 💥 4. Kinh Nghiệm Thực Chiến & Các Bẫy Cần Nhớ (Battle-Tested Lessons)

### ⚠️ Bẫy 1: Sửa UI/Responsive PHẢI dùng CSS, TUYỆT ĐỐI KHÔNG tự tiện sửa Template
- **Hiện tượng**: Khi thấy ô input bị hẹp chữ placeholder ("Nhập Mã" bị cụt thành "Nhậ"), lập trình viên/AI có xu hướng vội vàng gỡ bỏ thuộc tính `show-word-limit` hoặc `:maxlength="128"` trong template để lấy chỗ trống.
- **Hậu quả**: Vi phạm thiết kế nghiệp vụ của hệ thống (mất bộ đếm `0 / 128` so với Staging/Prod), làm sai lệch hành vi dữ liệu.
- **Nguyên tắc vàng (Safeguard 9 - Rule 19.27)**:
  - **Giữ nguyên 100% thuộc tính UI gốc của component**.
  - Mọi bài toán bố cục, tràn viền, responsive, che chữ **BẮT BUỘC GIẢI QUYẾT BẰNG CSS / SCSS**.
  - Nếu gặp không gian quá hẹp không thể xử lý thuần CSS: **Hỏi ý kiến người dùng trước** để xin phép lược bỏ, không tự ý xóa.

---

### ⚠️ Bẫy 2: Element Plus `clearable` dùng `visibility: hidden` gây chiếm diện tích ngầm
- **Bản chất**:
  - Khi bật `clearable`, Element Plus luôn sinh thẻ `<i class="el-input__clear">` trong `.el-input__suffix`.
  - Khi ô input trống hoặc chưa hover, Element Plus gán inline style:
    ```html
    <i class="el-input__clear" style="visibility: hidden;"></i>
    ```
  - Vì là `visibility: hidden` (không phải `display: none`), icon cùng margin của nó vẫn chiếm **~22px không gian ngang**. Kết hợp padding 11px mỗi bên, khoảng trống cho text bị thu hẹp đáng kể.
- **Cách xử lý CSS chuẩn**:
  ```scss
  /* Ẩn triệt để icon close khi nó đang ở trạng thái visibility: hidden */
  .el-input__clear[style*='visibility: hidden'] {
    display: none !important;
    width: 0 !important;
    margin: 0 !important;
  }
  ```
  *(Lưu ý: Không ẩn cả khối `.el-input__suffix` vì bên trong suffix còn có bộ đếm ký tự `0 / 128` (`.el-input__count`))*.

---

### ⚠️ Bẫy 3: Cạm bẫy Flexbox `min-width: 0` khi chia cột màn hình nhỏ
- **Hiện tượng**:
  - Khi form có `.search-form { flex: 1 1 0%; min-width: 0; }` và bên cạnh là cụm nút tìm kiếm `.search-btn-wrapper { flex-shrink: 0; }` (rộng ~180px).
  - Khi thu nhỏ màn hình hoặc chia 2 card song song (40% / 60%), card chỉ còn ~300px.
  - Flexbox thấy form có `min-width: 0` nên **ép form co lại chỉ còn 60px – 80px** để nhường chỗ cho 2 nút bên phải.
  - Form chia 2-3 cột $\rightarrow$ mỗi ô input bị bẹp dí thành một **nút bầu dục tí hon 15px**!
- **Giải pháp hiện đại: CSS Container Queries (`@container`)**:
  - Thay vì phụ thuộc cứng vào breakpoint toàn màn hình `@media` (vì trong cùng 1 màn hình 1920px, card 40% có chiều rộng khác hẳn card 60%), hãy để từng card **tự đo chiều rộng của chính nó**:
  ```scss
  .search-card {
    container-type: inline-size;

    /* Mặc định trên card hẹp: xếp dạng 2 tầng (inputs ở trên 100%, nút ở dưới) */
    .el-card__body {
      display: flex;
      flex-direction: column;
      width: 100%;
    }
    .search-btn-wrapper {
      width: 100%;
      justify-content: flex-end;
    }

    /* Khi chiều rộng THỰC TẾ của card đủ lớn (>= 480px hoặc 620px): tự động gom về 1 dòng */
    @container (min-width: 480px) {
      .el-card__body {
        flex-direction: row;
        align-items: center;
        flex-wrap: nowrap;
      }
      .search-btn-wrapper {
        width: auto;
      }
    }
  }
  ```

---

## 📌 5. Tóm Tắt Nhanh Cho Lập Trình Viên

1. **Khi chuyển từ React sang Vue 3**:
   - Quên đi việc "toàn bộ hàm chạy lại mỗi lần state đổi". Trong Vue 3 `<script setup>`, code khởi tạo chỉ chạy một lần.
   - Không cần mảng dependency `[]` cho `computed` (tự động track).
   - Truy cập giá trị của `ref` trong `<script>` phải qua `.value`, nhưng trong `<template>` thì viết thẳng biến không cần `.value`.
2. **Khi chuyển từ Vue 3 sang React**:
   - Cẩn thận với việc mutate state trực tiếp (luôn dùng setter và object spread `{ ...prev }`).
   - Mọi biến/hàm khai báo trong component sẽ được tạo mới ở mỗi render $\rightarrow$ cần `useCallback` / `useMemo` nếu truyền xuống component con tối ưu `React.memo`.
   - Chú ý stale closure trong `useEffect`.
3. **Khi fix lỗi giao diện trên bất kỳ framework nào**:
   - **Tôn trọng template gốc**: Không tự tiện xóa bỏ props nghiệp vụ (`maxlength`, `show-word-limit`, nhãn).
   - **CSS-First**: Sử dụng Container Queries, Flexbox wrap, `:has()`, pseudo-classes để layout tự thích ứng thông minh.
