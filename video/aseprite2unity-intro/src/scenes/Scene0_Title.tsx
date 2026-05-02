import { useCurrentFrame, useVideoConfig, interpolate, Easing } from "remotion";
import { DarkBackground } from "../components/DarkBackground";

export const Scene0_Title: React.FC = () => {
  const frame = useCurrentFrame();
  const { fps } = useVideoConfig();

  // Title entrance
  const titleOpacity = interpolate(frame, [fps * 0.3, fps * 1.5], [0, 1], {
    extrapolateLeft: "clamp",
    extrapolateRight: "clamp",
    easing: Easing.bezier(0.16, 1, 0.3, 1),
  });

  const titleScale = interpolate(frame, [fps * 0.3, fps * 1.5], [0.85, 1], {
    extrapolateLeft: "clamp",
    extrapolateRight: "clamp",
    easing: Easing.bezier(0.16, 1, 0.3, 1),
  });

  // Subtitle
  const subtitleOpacity = interpolate(frame, [fps * 1.0, fps * 2.0], [0, 1], {
    extrapolateLeft: "clamp",
    extrapolateRight: "clamp",
    easing: Easing.bezier(0.16, 1, 0.3, 1),
  });

  const subtitleY = interpolate(frame, [fps * 1.0, fps * 2.0], [20, 0], {
    extrapolateLeft: "clamp",
    extrapolateRight: "clamp",
    easing: Easing.bezier(0.16, 1, 0.3, 1),
  });

  // Bottom tag line
  const tagOpacity = interpolate(frame, [fps * 2.0, fps * 3.0], [0, 1], {
    extrapolateLeft: "clamp",
    extrapolateRight: "clamp",
    easing: Easing.bezier(0.16, 1, 0.3, 1),
  });

  // Pixel decoration dots
  const dotOpacity = interpolate(frame, [fps * 2.5, fps * 3.5], [0, 0.6], {
    extrapolateLeft: "clamp",
    extrapolateRight: "clamp",
  });

  return (
    <div style={{ position: "relative", width: "100%", height: "100%" }}>
      <DarkBackground />

      {/* Decorative pixel grid */}
      <div
        style={{
          position: "absolute",
          inset: 0,
          backgroundImage:
            "radial-gradient(circle, rgba(108,92,231,0.08) 1px, transparent 1px)",
          backgroundSize: "40px 40px",
          opacity: dotOpacity,
        }}
      />

      {/* Logo pixel art (CSS) */}
      <div
        style={{
          position: "absolute",
          top: "28%",
          left: "50%",
          transform: "translate(-50%, -50%)",
          display: "flex",
          gap: 12,
          opacity: titleOpacity,
        }}
      >
        <PixelBlock color="#6c5ce7" size={28} delay={0} />
        <PixelBlock color="#a29bfe" size={28} delay={0.1} />
        <PixelBlock color="#00cec9" size={28} delay={0.2} />
        <PixelBlock color="#6c5ce7" size={28} delay={0.3} />
        <PixelBlock color="#fd79a8" size={28} delay={0.4} />
      </div>

      {/* Main title */}
      <div
        style={{
          position: "absolute",
          top: "42%",
          left: "50%",
          transform: `translate(-50%, -50%) scale(${titleScale})`,
          opacity: titleOpacity,
          textAlign: "center",
        }}
      >
        <h1
          style={{
            fontFamily: '"Segoe UI", "Noto Sans SC", sans-serif',
            fontSize: 88,
            fontWeight: 800,
            color: "#ffffff",
            margin: 0,
            letterSpacing: -1,
            textShadow: "0 0 60px rgba(108,92,231,0.4)",
          }}
        >
          Aseprite2Unity-Ext
        </h1>
      </div>

      {/* Subtitle */}
      <div
        style={{
          position: "absolute",
          top: "52%",
          left: "50%",
          transform: `translate(-50%, -50%) translateY(${subtitleY}px)`,
          opacity: subtitleOpacity,
          textAlign: "center",
        }}
      >
        <p
          style={{
            fontFamily: '"Segoe UI", "Noto Sans SC", sans-serif',
            fontSize: 36,
            fontWeight: 400,
            color: "#a29bfe",
            margin: 0,
            letterSpacing: 4,
          }}
        >
          Unity 精灵动画导入工具
        </p>
      </div>

      {/* Bottom tags */}
      <div
        style={{
          position: "absolute",
          bottom: 60,
          left: "50%",
          transform: "translateX(-50%)",
          opacity: tagOpacity,
          display: "flex",
          gap: 32,
        }}
      >
        {["v2.0.0", "MIT License", "开源免费"].map((tag) => (
          <span
            key={tag}
            style={{
              fontFamily: '"JetBrains Mono", monospace',
              fontSize: 18,
              color: "rgba(255,255,255,0.5)",
              border: "1px solid rgba(255,255,255,0.15)",
              borderRadius: 6,
              padding: "6px 16px",
            }}
          >
            {tag}
          </span>
        ))}
      </div>
    </div>
  );
};

const PixelBlock: React.FC<{ color: string; size: number; delay: number }> = ({
  color,
  size,
  delay,
}) => {
  const frame = useCurrentFrame();
  const { fps } = useVideoConfig();

  const scale = interpolate(
    frame,
    [fps * delay, fps * delay + fps * 0.5],
    [0.3, 1],
    {
      extrapolateLeft: "clamp",
      extrapolateRight: "clamp",
      easing: Easing.bezier(0.34, 1.56, 0.64, 1),
    },
  );

  return (
    <div
      style={{
        width: size,
        height: size,
        backgroundColor: color,
        borderRadius: 4,
        transform: `scale(${scale})`,
        boxShadow: `0 0 20px ${color}66`,
      }}
    />
  );
};
