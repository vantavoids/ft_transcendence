import type { Metadata } from 'next';
import Link from 'next/link';
import { LegalPage } from '../../src/components/legal-page';

export const metadata: Metadata = {
  title: 'Privacy Policy — ft_discord',
  description: 'How ft_discord collects, uses, and protects your personal data under the GDPR.'
};

export default function PrivacyPage() {
  return (
    <LegalPage eyebrow="ft_discord — legal" title="Privacy Policy" lastUpdated="7 July 2026">
      <p>
        This document explains what personal data ft_discord processes, why, for how long, and how
        you can exercise your rights under the GDPR.
      </p>

      <h2>1. Who is responsible for your data</h2>
      <p>
        ft_discord is a non-commercial student project built by the ft_discord team to satisfy the
        ft_transcendence project of the École 42 common core. The data controller for this policy is
        Yanis Andry, contactable at <a href="mailto:yandry@student.42.fr">yandry@student.42.fr</a>.
        There is no separate corporate entity behind the Service.
      </p>

      <h2>2. What data we collect</h2>
      <p>We collect the following personal data that you provide directly:</p>
      <ul>
        <li>
          Your email address and password (hashed; we never store or have access to the plaintext),
          or, if you register through GitHub, Google, or the 42 intranet, the identifier and profile
          information that provider shares with us
        </li>
        <li>Your username, display name, avatar, banner, and bio</li>
        <li>The guilds, channels, and categories you create, including their names and icons</li>
        <li>Messages you send, and any files you attach to them</li>
        <li>Your friends list and the users you block</li>
        <li>
          The reason you give when banning another user in a guild you moderate (see Section 5 for
          how this is handled if your account is later deleted)
        </li>
      </ul>
      <p>
        If you sign in through a third-party provider (GitHub, Google, 42), that provider&rsquo;s
        own terms of service and privacy policy govern the data you share with them; we only receive
        what you authorize them to disclose to us.
      </p>
      <p>We do not use third-party analytics, advertising, or tracking services.</p>
      <p>
        We also process your IP address at the point each request is made, to apply rate limiting
        and prevent abuse of the Service. This is not retained for longer than necessary for that
        security purpose.
      </p>
      <p>
        <em>Legal basis:</em> performance of a contract, and, for optional fields such as bio or
        avatar, your consent in choosing to provide them. Processing your IP address for rate
        limiting is based on our legitimate interest in keeping the Service secure and available.
      </p>

      <h2>3. Cookies</h2>
      <p>
        The Service sets cookies solely for technical purposes required to keep you signed in. They
        are not used for tracking, profiling, or advertising.
      </p>

      <h2>4. How long we keep your data</h2>
      <p>
        Your data is kept for as long as your account is active. If you stop using the Service
        without deleting your account, your data simply remains as-is; there is currently no
        automatic inactivity-based deletion.
      </p>

      <h2>5. What happens when you delete your account</h2>
      <p>Deleting your account triggers the following:</p>
      <ul>
        <li>
          Your login credentials are deactivated immediately and your session is revoked. Your email
          is freed for re-registration.
        </li>
        <li>Your profile, friendships, and blocks are deleted outright.</li>
        <li>
          Your guild memberships, role assignments, invite codes, and channel-level permission
          overrides are deleted. Guilds you <em>own</em> cannot be deleted this way, you must
          transfer ownership or delete the guild first before your account can be removed.
        </li>
        <li>
          If you&rsquo;ve banned other users, the ban itself stays in force (it protects the guild,
          not you), but the record of <em>who issued it</em> is erased.
        </li>
        <li>Your notifications, and any notification triggered by your actions, are deleted.</li>
        <li>
          Your messages are <strong>not</strong> deleted. They remain visible to other participants
          in the channel or conversation, but your identity becomes unresolvable, the interface will
          show them as sent by &ldquo;Deleted User.&rdquo; This preserves conversation context for
          other users while removing your identifying information from the record.
        </li>
      </ul>
      <p>
        This reference-scrubbing on deletion is the Service&rsquo;s anonymization mechanism: your
        identity is removed from content and records that other users depend on, without deleting
        content those users have a legitimate interest in retaining (a conversation, a ban record).
        It is triggered by account deletion rather than offered as a separate action, by design: it
        protects your identity, not your content, so there is no meaningful &ldquo;anonymize but
        keep the account&rdquo; state to offer.
      </p>

      <h2>6. Your rights</h2>
      <p>Under the GDPR, you have the right to:</p>
      <ul>
        <li>
          <strong>Access</strong> the data we hold about you
        </li>
        <li>
          <strong>Rectify</strong> inaccurate data (most profile fields are user-editable directly
          in the app)
        </li>
        <li>
          <strong>Erase</strong> your data (see Section 5, account deletion)
        </li>
        <li>
          <strong>Restrict or object to</strong> processing
        </li>
        <li>
          <strong>Data portability</strong>, receive your data in a structured format
        </li>
      </ul>
      <p>
        You can access and download your data yourself at any time using the &ldquo;Download my
        data&rdquo; button in the app. For any right not covered by that self-service flow
        (rectification of data you can&rsquo;t edit directly, restriction, or objection), contact{' '}
        <a href="mailto:yandry@student.42.fr">yandry@student.42.fr</a>.
      </p>
      <p>
        We respond to any request under this section within 30 days, as required by GDPR Article
        12(3).
      </p>

      <h2>7. Security</h2>
      <p>
        Passwords are hashed and never stored in plaintext. Authentication tokens are short-lived
        and rotated regularly to limit the impact of any compromise.
      </p>

      <h2>8. Children</h2>
      <p>
        The Service does not knowingly collect data from children under 15 without the parental
        consent required under French law, and does not target children as an audience. See the{' '}
        <Link href="/terms">Terms of Service</Link> for the eligibility statement.
      </p>

      <h2>9. Changes to this policy</h2>
      <p>
        This policy will be updated as the application&rsquo;s data handling changes. Material
        changes will be reflected here with an updated date.
      </p>

      <h2>10. Contact</h2>
      <p>
        <a href="mailto:yandry@student.42.fr">yandry@student.42.fr</a>
      </p>
      <p>
        You also have the right to lodge a complaint with the CNIL (Commission Nationale de
        l&rsquo;Informatique et des Libertés), the French data protection authority, at{' '}
        <a href="https://www.cnil.fr" target="_blank" rel="noreferrer">
          www.cnil.fr
        </a>
        .
      </p>
    </LegalPage>
  );
}
