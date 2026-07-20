## Brief overview
- Project-specific guideline for React Native `Animated.Value` initialization in function components.
- Prevent ESLint `Cannot access refs during render` warnings caused by patterns like `useRef(new Animated.Value(1)).current`.

## Animated.Value initialization
- Use `useState` with lazy initialization for stable `Animated.Value` instances:
  ```tsx
  const [pulseAnim] = useState(() => new Animated.Value(1));
  ```
- Do not initialize animated values with `useRef(...).current` when the value is consumed in render styles:
  ```tsx
  // Avoid
  const pulseAnim = useRef(new Animated.Value(1)).current;
  ```
- Include the animated value in hook dependency arrays when used inside `useEffect`:
  ```tsx
  useEffect(() => {
    const animation = Animated.loop(
      Animated.timing(pulseAnim, {
        toValue: 1,
        duration: 300,
        useNativeDriver: true,
      })
    );

    animation.start();
    return () => animation.stop();
  }, [pulseAnim]);
  ```

## Trigger cases
- Apply this rule when creating pulse, scale, opacity, translate, rotate, or other persistent `Animated.Value` instances in React Native components.
- Apply this rule when ESLint reports `Cannot access refs during render` for `useRef(new Animated.Value(...)).current`.
- Apply this rule when an animated value is passed into JSX style props such as:
  ```tsx
  <Animated.View style={{ transform: [{ scale: pulseAnim }] }} />
  ```

## Coding best practices
- Prefer concise Vietnamese comments only when the reason is not obvious, for example:
  ```tsx
  // Dùng useState để tránh ESLint "Cannot access refs during render"
  ```
- Do not suppress the ESLint rule with inline disable comments for this case.
- Keep animation cleanup inside `useEffect` to avoid orphaned loops or timers.