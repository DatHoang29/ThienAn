## Brief overview
  Hướng dẫn về việc định nghĩa functions bên trong component thay vì đặt bên ngoài. Áp dụng cho React Native/React trong dự án aicc_mobile.

## Quy tắc chính
  - Nếu function chỉ dùng cho một component duy nhất → định nghĩa bên trong component đó.
  - Nếu function dùng cho nhiều component khác nhau → viết helper bên ngoài (tái sử dụng được).
  - Sử dụng `useCallback` khi cần thiết để tránh re-renders không mong muốn.

## Trigger cases
  - Function chỉ được gọi trong một component duy nhất.
  - Function có phụ thuộc vào state/props của component đó.

## Ví dụ
  /// Thay vì (đặt bên ngoài):
  const getChannelIcon = (channel: string) => { ... };
  function MyComponent() { ... }

  /// Nên là (đặt bên trong vì chỉ dùng trong component này):
  function MyComponent() {
    const getChannelIcon = useCallback((channel: string) => { ... }, []);
    ...
  }
