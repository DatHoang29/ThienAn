## Brief overview
Hướng dẫn tránh gọi `setState` đồng bộ trong `useEffect` để ngăn chặn hiện tượng cascading renders (re-render tầng tầng lớp lớp) và cảnh báo hiệu năng trong React/React Native.

## Quy tắc chung (General principles)
- **Không lạm dụng useEffect để đồng bộ state**: Nếu một state B có thể được tính toán trực tiếp từ state A hoặc props, hãy tính toán trực tiếp trong quá trình render (hoặc dùng `useMemo` nếu việc tính toán tốn hiệu năng), thay vì dùng `useEffect` để cập nhật state B khi state A thay đổi.
- **Cập nhật state tại các Event Handlers**: Khi cần reset hoặc thay đổi state (ví dụ: reset trang quay lại 1, reset giới hạn hiển thị `displayLimit` về mặc định) khi bộ lọc hoặc ô tìm kiếm thay đổi, hãy thực hiện việc này trực tiếp trong các hàm xử lý sự kiện (event handlers) thay đổi bộ lọc/ô tìm kiếm đó, thay vì dùng `useEffect` để lắng nghe thay đổi của bộ lọc rồi mới gọi `setState`.

## Cách giải quyết và Code mẫu (Refactoring patterns)
1. **Tính toán trực tiếp trong render (Derived state)**:
   * Tránh:
     ```tsx
     useEffect(() => {
       setFilteredItems(items.filter(item => item.active));
     }, [items]);
     ```
   * Nên dùng:
     ```tsx
     const filteredItems = items.filter(item => item.active);
     ```

2. **Cập nhật đồng thời trong Event Handler**:
   * Tránh:
     ```tsx
     useEffect(() => {
       setDisplayLimit(3);
     }, [searchQuery]);
     ```
   * Nên dùng:
     ```tsx
     const handleSearchChange = (query: string) => {
       setSearchQuery(query);
       setDisplayLimit(3); // Cập nhật đồng thời tại nơi phát sinh sự kiện
     };
     ```

3. **Cập nhật state trực tiếp khi render (Adjusting state during render)**:
   * Trong trường hợp bắt buộc phải đồng bộ state dựa trên sự thay đổi của prop/state khác mà không thể làm ở event handler, hãy dùng cơ chế lưu vết giá trị trước đó và setState trực tiếp trong thân component (nhưng phải có điều kiện bảo vệ để tránh lặp vô hạn):
     ```tsx
     const [prevQuery, setPrevQuery] = useState(query);
     const [displayLimit, setDisplayLimit] = useState(3);

     if (query !== prevQuery) {
       setPrevQuery(query);
       setDisplayLimit(3); // React sẽ re-render ngay lập tức trước khi paint UI
     }
     ```

## Các trường hợp áp dụng (Trigger cases)
- Cảnh báo "Avoid calling setState() directly within an effect" hoặc "Calling setState synchronously within an effect".
- Reset giới hạn trang (`page`, `displayLimit`), trạng thái cuộn, trạng thái mở rộng khi tab, bộ lọc hoặc ô tìm kiếm thay đổi.