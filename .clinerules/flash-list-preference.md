## Brief overview
Project-specific rule for aicc_mobile: Always use `@shopify/flash-list` instead of `FlatList` for rendering lists with items.

## Rule
- **Luôn dùng `FlashList`** từ `@shopify/flash-list` cho mọi danh sách cần render items
- **Không dùng `FlatList`** trừ khi FlashList không hỗ trợ tính năng cần thiết
- FlashList đã được cài đặt: `@shopify/flash-list: ^2.3.2`

## Usage
```tsx
import { FlashList } from "@shopify/flash-list";

<FlashList
  data={items}
  renderItem={({ item }) => <ItemComponent item={item} />}
  estimatedItemSize={60}
/>
```

## Trigger cases
- Tạo mới component hiển thị danh sách items
- Refactor code có dùng FlatList
- Viết UI cho list trong bất kỳ feature nào của aicc_mobile