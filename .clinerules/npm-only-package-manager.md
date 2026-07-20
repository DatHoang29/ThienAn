## Brief overview
Dự án `aicc_mobile` chỉ sử dụng **npm** làm package manager duy nhất. Không sử dụng pnpm, yarn, hay bất kỳ package manager nào khác cho dự án này.

## Package manager rules
- **Luôn dùng `npm`** cho mọi thao tác: `npm install`, `npm run <script>`, `npx <command>`, v.v.
- **Không dùng `pnpm`** hoặc `yarn`在任何 tình huống nào liên quan đến `aicc_mobile/`.
- Scripts trong `package.json` đã được cấu hình dùng npm (ví dụ: `"clear": "npm run remove && npm i && expo -c"`).

## Trigger cases
- Khi cài đặt dependencies: dùng `npm install` thay vì `pnpm install` hoặc `yarn add`.
- Khi chạy script: dùng `npm run <script>` thay vì `pnpm run <script>`.
- Khi thêm devDependencies: dùng `npm install --save-dev <package>`.
- Khi global tools: dùng `npx <tool>` thay vì `pnpm dlx`.

## Lưu ý
- Thư mục root của monorepo (`aicc-fslk/`) có thể dùng `pnpm`, nhưng `aicc_mobile/` là project riêng chỉ dùng npm.
- File `package-lock.json` là lock file chính thức của dự án.