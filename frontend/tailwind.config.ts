import type { Config } from 'tailwindcss';

const config: Config = {
  content: ['./app/**/*.{ts,tsx}', './components/**/*.{ts,tsx}', './src/**/*.{ts,tsx}'],
  theme: {
    extend: {
      fontSize: {
        xs: ['0.7rem', { lineHeight: '1rem' }],
        sm: ['0.8rem', { lineHeight: '1.15rem' }],
        base: ['0.9rem', { lineHeight: '1.35rem' }],
        lg: ['1rem', { lineHeight: '1.5rem' }],
        xl: ['1.1rem', { lineHeight: '1.6rem' }],
        '2xl': ['1.25rem', { lineHeight: '1.75rem' }],
        '3xl': ['1.5rem', { lineHeight: '1.9rem' }],
        '4xl': ['1.8rem', { lineHeight: '2.1rem' }],
        '5xl': ['2.2rem', { lineHeight: '1' }],
        '6xl': ['2.8rem', { lineHeight: '1' }],
        '7xl': ['3.4rem', { lineHeight: '1' }],
        '8xl': ['4.2rem', { lineHeight: '1' }],
        '9xl': ['5rem', { lineHeight: '1' }]
      },
      fontFamily: {
        sans: ['Inter', 'ui-sans-serif', 'system-ui', 'sans-serif'],
        display: ['Inter', 'ui-sans-serif', 'system-ui', 'sans-serif'],
        category: ['Outfit', 'Inter', 'ui-sans-serif', 'system-ui', 'sans-serif'],
        mono: ['JetBrains Mono', 'ui-monospace', 'SFMono-Regular', 'monospace']
      },
      spacing: {
        120: '30rem',
        140: '35rem'
      },
      colors: {
        'primary-bg': '#191919',
        'secondary-bg': '#09090b',
        panel: '#121213',
        frame: '#1a1a1c',
        stroke: '#242426',
        'grey-link': '#848485',
        muted: '#5b5b5c',
        category: '#545454',
        'input-bg': '#0f0f10',
        'input-placeholder': '#343434',
        aqua: '#78dce8',
        lavender: '#ab9df2',
        pink: '#ff6188',
        orange: '#fc9867',
        yellow: '#ffd866',
        lime: '#a9dc76'
      },
      boxShadow: {
        glow: '0 0 40px rgba(169, 220, 118, 0.28), 0 0 80px rgba(120, 220, 232, 0.2), 0 0 120px rgba(255, 97, 136, 0.16)'
      }
    }
  },
  plugins: []
};

export default config;
